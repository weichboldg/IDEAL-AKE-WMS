# FA-Abarbeitungsliste: Komma-Werkbank-Filter + Bezeichnungs-Spalten — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `User.DefaultWorkplaceId` (Einzel-FK) durch `User.DefaultWorkbenches` (komma-separierter String) ersetzen — Textfeld statt Dropdown in Profil + Benutzerstamm, Komma-Contains-Filter in der FA-Abarbeitungsliste — und zusätzlich Bezeichnung 1 + 2 als Spalten anzeigen.

**Architecture:** Datenmodell-Rename `DefaultWorkplaceId` → `DefaultWorkbenches` (Migration 75, destruktiv) quer durch Model/EF/ViewModels/Controller/Views. Die Filter-Semantik (komma-OR, contains, case-insensitiv) kapselt eine reine Helper-Klasse `WorkbenchFilter` (testbar). Der FaWorklist-Controller wendet den User-Default an, wenn der `workbenches`-Query-Param fehlt; ein vorhandener (auch leerer) Param überschreibt. Die Bezeichnungs-Spalten sind additiv (Row + View + ColumnMap).

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10 (SQL Server), xUnit + FluentAssertions + Moq + EF InMemory, Razor.

**Branch / Worktree:** `feature/windows-auth-ad-users` im Worktree `.claude/worktrees/missingparts-include-pd`. Spec: [docs/superpowers/specs/2026-06-19-faworklist-werkbankfilter-bezeichnung-design.md](../specs/2026-06-19-faworklist-werkbankfilter-bezeichnung-design.md).

**WICHTIG (Standing Constraints):** KEIN Merge nach `main`, KEIN Worktree-/Branch-Cleanup ohne ausdrückliche User-Freigabe. Plan endet mit grünem Build + Tests auf dem Branch.

---

## File Structure

**Neu:**
- `IdealAkeWms/Services/WorkbenchFilter.cs` — reine Filter-Semantik (`Matches`).
- `IdealAkeWms.Tests/Services/WorkbenchFilterTests.cs` — Unit-Tests dazu.
- `IdealAkeWms/Migrations/<timestamp>_ReplaceUserDefaultWorkplaceWithWorkbenches.cs` (EF-generiert).
- `SQL/75_ReplaceUserDefaultWorkplaceWithWorkbenches.sql`.

**Geändert:**
- `IdealAkeWms/Models/User.cs` — `DefaultWorkplaceId`/`DefaultWorkplace` → `DefaultWorkbenches`.
- `IdealAkeWms/Data/ApplicationDbContext.cs` — FK-Config raus, `DefaultWorkbenches`-Length rein.
- `IdealAkeWms/Models/ViewModels/ProfileViewModel.cs`, `UserEditViewModel.cs`, `FaWorklistViewModel.cs`.
- `IdealAkeWms/Controllers/AccountController.cs`, `UsersController.cs`, `FaWorklistController.cs`.
- `IdealAkeWms/Views/Account/Profile.cshtml`, `Views/Users/Edit.cshtml`, `Views/FaWorklist/Index.cshtml`.
- `SQL/00_FreshInstall.sql`.
- `IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs`.
- `IdealAkeWms/Views/Help/Changelog.cshtml`, `CLAUDE.md`, `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md`.

---

## Task 1: `WorkbenchFilter` Helper (TDD)

**Files:**
- Create: `IdealAkeWms/Services/WorkbenchFilter.cs`
- Test: `IdealAkeWms.Tests/Services/WorkbenchFilterTests.cs`

- [ ] **Step 1: Failing tests schreiben**

`IdealAkeWms.Tests/Services/WorkbenchFilterTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class WorkbenchFilterTests
{
    [Theory]
    [InlineData(null, null, true)]            // kein Filter -> alle
    [InlineData("Werkbank 1", null, true)]    // leerer Filter -> alle
    [InlineData("Werkbank 1", "", true)]      // leerer Filter -> alle
    [InlineData("Werkbank 1", "   ", true)]   // nur Whitespace -> alle
    [InlineData(null, "Werkbank 1", false)]   // Name fehlt, Filter gesetzt -> kein Treffer
    public void Matches_EmptyCases(string? name, string? filter, bool expected)
        => WorkbenchFilter.Matches(name, filter).Should().Be(expected);

    [Theory]
    [InlineData("Werkbank 1", "Werkbank 1", true)]
    [InlineData("Werkbank 2", "Werkbank 1", false)]
    [InlineData("WB-A2", "WB-A", true)]                 // Contains (Teilstring)
    [InlineData("WB-A", "wb-a", true)]                  // case-insensitiv
    [InlineData("Halle 3", "WB-A,Halle 3", true)]       // Komma-OR
    [InlineData("WB-B", "WB-A,Halle 3", false)]
    [InlineData("WB-A", " WB-A , WB-B ", true)]         // Tokens getrimmt
    public void Matches_ContainsSemantics(string? name, string? filter, bool expected)
        => WorkbenchFilter.Matches(name, filter).Should().Be(expected);
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WorkbenchFilterTests"`
Expected: FAIL — `WorkbenchFilter` existiert nicht.

- [ ] **Step 3: Helper implementieren**

`IdealAkeWms/Services/WorkbenchFilter.cs`:

```csharp
namespace IdealAkeWms.Services;

/// <summary>
/// Werkbank-Filter fuer die FA-Abarbeitungsliste: komma-separierte Tokens, Treffer wenn der
/// Werkbank-Name (case-insensitiv) EINEN der Tokens ENTHAELT (Komma-OR). Leerer/Null-Filter =
/// alle. Gleiche Contains-Semantik wie der `workbench`-Spaltenfilter (ColumnFilterHelper).
/// </summary>
public static class WorkbenchFilter
{
    public static bool Matches(string? workplaceName, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;

        var tokens = filter
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Where(t => t.Length > 0)
            .ToList();
        if (tokens.Count == 0) return true;

        var name = (workplaceName ?? string.Empty).ToLowerInvariant();
        return tokens.Any(t => name.Contains(t));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WorkbenchFilterTests"`
Expected: PASS (alle 12 Theory-Fälle grün).

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Services/WorkbenchFilter.cs IdealAkeWms.Tests/Services/WorkbenchFilterTests.cs
git commit -m "feat(faworklist): WorkbenchFilter (komma-OR contains) + Tests"
```

---

## Task 2: Rename `DefaultWorkplaceId` → `DefaultWorkbenches` + FaWorklist-Filter

> Cohärente Multi-Layer-Änderung: erst nach allen Teilschritten kompiliert das Projekt wieder.
> Ein Commit am Ende. Die EF-Migration kommt in Task 3 (Model muss vorher kompilieren).

**Files:**
- Modify: `IdealAkeWms/Models/User.cs:70-78`
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs:87-95`
- Modify: `IdealAkeWms/Models/ViewModels/ProfileViewModel.cs:48-55`, `UserEditViewModel.cs:60-67`, `FaWorklistViewModel.cs`
- Modify: `IdealAkeWms/Controllers/AccountController.cs` (Profile GET ~126, POST ~160)
- Modify: `IdealAkeWms/Controllers/UsersController.cs` (Edit GET ~233, POST ~273)
- Modify: `IdealAkeWms/Controllers/FaWorklistController.cs` (Index 64-148)
- Modify: `IdealAkeWms/Views/Account/Profile.cshtml:107-118`, `Views/Users/Edit.cshtml:136-146`, `Views/FaWorklist/Index.cshtml:46-55`
- Modify: `IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs` (SeedUser + 2 Tests + 3 neue Tests)

- [ ] **Step 1: User-Model**

In `IdealAkeWms/Models/User.cs` den `DefaultWorkplaceId`-Block (die 4 Zeilen: Comment + `[Display]` + `DefaultWorkplaceId` + `DefaultWorkplace`-Nav) ersetzen durch:

```csharp
    /// <summary>Standard-Werkbaenke (kommasepariert) fuer die FA-Abarbeitungsliste (NULL/leer = alle).</summary>
    [Display(Name = "Standard-Werkbaenke (FA-Abarbeitungsliste, kommasepariert)")]
    public string? DefaultWorkbenches { get; set; }
```

(`DefaultWorkStepId` + `DefaultWorkStep` BLEIBEN unverändert.)

- [ ] **Step 2: EF-Konfiguration**

In `IdealAkeWms/Data/ApplicationDbContext.cs` den `DefaultWorkplace`-FK-Block (Zeilen 92-95) ersetzen durch:

```csharp
            entity.Property(e => e.DefaultWorkbenches).HasMaxLength(400);
```

(Der `DefaultWorkStep`-Block Zeilen 87-90 bleibt.)

- [ ] **Step 3: ViewModels**

In `IdealAkeWms/Models/ViewModels/ProfileViewModel.cs` den `DefaultWorkplaceId`-Block (Display + Property, Zeilen 48-52, NICHT `AvailableWorkplaces`) ersetzen durch:

```csharp
    /// <summary>
    /// Standard-Werkbaenke (kommasepariert) fuer die FA-Abarbeitungsliste. Leer = alle.
    /// </summary>
    [Display(Name = "Standard-Werkbaenke (FA-Abarbeitungsliste, kommasepariert)")]
    public string? DefaultWorkbenches { get; set; }
```

Identisch in `UserEditViewModel.cs` (Zeilen 60-64). `AvailableWorkplaces` BLEIBT in beiden (Datalist-Quelle).

In `FaWorklistViewModel.cs` die Zeile
```csharp
    public int? SelectedWorkplaceId { get; set; }                              // Zusatzfilter Werkbank (NULL = alle)
```
ersetzen durch:
```csharp
    public string? Workbenches { get; set; }                                   // Komma-Werkbank-Filter (NULL/leer = alle)
```
(`AvailableWorkplaces` bleibt — Datalist.)

- [ ] **Step 4: AccountController.Profile**

In `IdealAkeWms/Controllers/AccountController.cs` im **GET** die ViewModel-Zeile
`DefaultWorkplaceId = user.DefaultWorkplaceId,` ersetzen durch
`DefaultWorkbenches = user.DefaultWorkbenches,`.

Im **POST** die Zeile `user.DefaultWorkplaceId = vm.DefaultWorkplaceId;` ersetzen durch:
```csharp
        user.DefaultWorkbenches = string.IsNullOrWhiteSpace(vm.DefaultWorkbenches) ? null : vm.DefaultWorkbenches.Trim();
```
(`AvailableWorkplaces`-Befüllung in GET + im ModelState-invalid-Zweig bleibt unverändert.)

- [ ] **Step 5: UsersController.Edit**

In `IdealAkeWms/Controllers/UsersController.cs` im **Edit GET** die ViewModel-Zeile
`DefaultWorkplaceId = user.DefaultWorkplaceId,` ersetzen durch
`DefaultWorkbenches = user.DefaultWorkbenches,`.

Im **Edit POST** die Zeile `existing.DefaultWorkplaceId = vm.DefaultWorkplaceId;` ersetzen durch:
```csharp
        existing.DefaultWorkbenches = string.IsNullOrWhiteSpace(vm.DefaultWorkbenches) ? null : vm.DefaultWorkbenches.Trim();
```
(`PopulateRolesAsync` befüllt `AvailableWorkplaces` weiter — unverändert.)

- [ ] **Step 6: FaWorklistController.Index — Signatur + Default-Logik + Filter**

In `IdealAkeWms/Controllers/FaWorklistController.cs`: oben `using IdealAkeWms.Services;` ist bereits vorhanden (`PageSize`/`ColumnFilterHelper`). Die Signatur (Zeilen 64-69) ersetzen:

```csharp
    public async Task<IActionResult> Index(
        int? workStepId,
        string? workbenches = null,
        bool showDone = false,
        int page = 1,
        int? pageSize = null)
```

Den Default-Fallback-Block (Zeilen 88-106, `if (workStepId == null || workplaceId == null) { ... }`) ersetzen durch:

```csharp
        // Schritt 2: Defaults aus dem aktuellen User. WorkStep-Default greift wenn ?workStepId fehlt.
        // Werkbank-Filter: explizit, wenn der Query-Param "workbenches" vorhanden ist (auch leer =
        // "alle"); sonst User.DefaultWorkbenches. (Param-Wert kann durch leeres GET-Feld null sein,
        // daher zusaetzlich Query.ContainsKey pruefen.)
        bool workbenchesProvided = workbenches != null
            || (HttpContext?.Request?.Query.ContainsKey("workbenches") ?? false);
        string? effectiveWorkbenches = workbenchesProvided ? (workbenches ?? string.Empty) : null;

        if (workStepId == null || !workbenchesProvided)
        {
            var appUserId = _currentUser.GetCurrentAppUserId();
            if (appUserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(appUserId.Value);
                if (workStepId == null)
                {
                    workStepId = user?.DefaultWorkStepId;
                }
                if (!workbenchesProvided)
                {
                    effectiveWorkbenches = user?.DefaultWorkbenches;
                }
            }
        }
```

Im ViewModel-Initializer (Zeilen 108-122) die Zeile `SelectedWorkplaceId = workplaceId,` ersetzen durch `Workbenches = effectiveWorkbenches,`.

Den Order-Filter (Zeilen 144-149) ersetzen durch:

```csharp
        var orders = (await _productionOrderRepository.GetAllOrderedAsync())
            .Where(o => !o.IsDone
                        && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking)
                        && orderIdsWithStep.Contains(o.Id)
                        && WorkbenchFilter.Matches(o.ProductionWorkplace?.Name, effectiveWorkbenches))
            .ToList();
```

- [ ] **Step 7: Profile.cshtml + Users/Edit.cshtml — Textfeld + Datalist**

In `IdealAkeWms/Views/Account/Profile.cshtml` den `DefaultWorkplaceId`-Block (Zeilen 107-118, das `<div class="mb-3">` mit dem `<select asp-for="DefaultWorkplaceId">`) ersetzen durch:

```html
                    <div class="mb-3">
                        <label asp-for="DefaultWorkbenches" class="form-label"></label>
                        <input asp-for="DefaultWorkbenches" class="form-control" list="workbenchOptions"
                               placeholder="z. B. Werkbank 1, Werkbank 2" />
                        <datalist id="workbenchOptions">
                            @foreach (var wp in Model.AvailableWorkplaces)
                            {
                                <option value="@wp.Name"></option>
                            }
                        </datalist>
                        <div class="form-text">
                            Standard-Werkbänke für die FA-Abarbeitungsliste, kommasepariert. Leer = alle.
                        </div>
                    </div>
```

In `IdealAkeWms/Views/Users/Edit.cshtml` den analogen `DefaultWorkplaceId`-Block (Zeilen 136-146) durch denselben `<div>` ersetzen (Einrückung an die Datei anpassen).

- [ ] **Step 8: FaWorklist/Index.cshtml — Filter-Input**

In `IdealAkeWms/Views/FaWorklist/Index.cshtml` den Werkbank-`<div class="col-md-4">`-Block (Zeilen 46-55, mit `<select name="workplaceId">`) ersetzen durch:

```html
                <div class="col-md-4">
                    <label class="form-label" for="workbenches">Werkbänke (kommasepariert)</label>
                    <input type="text" name="workbenches" id="workbenches" class="form-control" list="workbenchOptions"
                           value="@Model.Workbenches" placeholder="alle" onchange="this.form.submit()" />
                    <datalist id="workbenchOptions">
                        @foreach (var wp in Model.AvailableWorkplaces)
                        {
                            <option value="@wp.Name"></option>
                        }
                    </datalist>
                </div>
```

- [ ] **Step 9: Test-Helper + bestehende Tests anpassen + neue Tests**

In `IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs`:

(a) `SeedUser` (Zeilen 117-132): Signatur + Body ändern:

```csharp
    private static User SeedUser(ApplicationDbContext ctx, string name,
        int? defaultWorkStepId = null, string? defaultWorkbenches = null)
    {
        var user = new User
        {
            Name = name,
            DefaultWorkStepId = defaultWorkStepId,
            DefaultWorkbenches = defaultWorkbenches,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.Users.Add(user);
        ctx.SaveChanges();
        return user;
    }
```

(b) `Index_FiltersByWorkplace_WhenSet` (Zeilen 187-214) ersetzen durch:

```csharp
    [Fact]
    public async Task Index_FiltersByWorkbenches_WhenSet()
    {
        var (ctx, ctrl, _) = Build();
        var wp1 = SeedWorkplace(ctx, "Werkbank 1");
        var wp2 = SeedWorkplace(ctx, "Werkbank 2");
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 1);

        var o1 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var o2 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-002");
        o1.Order.ProductionWorkplaceId = wp1.Id;
        o2.Order.ProductionWorkplaceId = wp2.Id;
        ctx.SaveChanges();

        SeedFaWorkStep(ctx, o1.Order.Id, ve.Id);
        SeedFaWorkStep(ctx, o2.Order.Id, ve.Id);

        var result = await ctrl.Index(ve.Id, workbenches: "Werkbank 1");

        var vm = (FaWorklistViewModel)((ViewResult)result).Model!;
        vm.Workbenches.Should().Be("Werkbank 1");
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-001");
    }
```

(c) `Index_UsesUserDefaultWorkplace_WhenNoParam` (Zeilen 216-244) ersetzen durch:

```csharp
    [Fact]
    public async Task Index_UsesUserDefaultWorkbenches_WhenNoParam()
    {
        // Ohne ?workbenches greift User.DefaultWorkbenches als Filter.
        var (ctx, ctrl, userMock) = Build();
        var wp1 = SeedWorkplace(ctx, "Werkbank 1");
        var wp2 = SeedWorkplace(ctx, "Werkbank 2");
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 1);
        var user = SeedUser(ctx, "vorbau1", defaultWorkStepId: ve.Id, defaultWorkbenches: "Werkbank 2");
        userMock.Setup(x => x.GetCurrentAppUserId()).Returns(user.Id);

        var o1 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var o2 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-002");
        o1.Order.ProductionWorkplaceId = wp1.Id;
        o2.Order.ProductionWorkplaceId = wp2.Id;
        ctx.SaveChanges();

        SeedFaWorkStep(ctx, o1.Order.Id, ve.Id);
        SeedFaWorkStep(ctx, o2.Order.Id, ve.Id);

        var result = await ctrl.Index(null);

        var vm = (FaWorklistViewModel)((ViewResult)result).Model!;
        vm.SelectedWorkStepId.Should().Be(ve.Id);
        vm.Workbenches.Should().Be("Werkbank 2");
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-002");
    }
```

(d) Drei neue Tests am Ende der Klasse (vor schließender `}`) hinzufügen:

```csharp
    [Fact]
    public async Task Index_ExplicitWorkbenches_OverridesUserDefault()
    {
        var (ctx, ctrl, userMock) = Build();
        var wp1 = SeedWorkplace(ctx, "Werkbank 1");
        var wp2 = SeedWorkplace(ctx, "Werkbank 2");
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 1);
        var user = SeedUser(ctx, "vorbau1", defaultWorkStepId: ve.Id, defaultWorkbenches: "Werkbank 1");
        userMock.Setup(x => x.GetCurrentAppUserId()).Returns(user.Id);

        var o1 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var o2 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-002");
        o1.Order.ProductionWorkplaceId = wp1.Id;
        o2.Order.ProductionWorkplaceId = wp2.Id;
        ctx.SaveChanges();
        SeedFaWorkStep(ctx, o1.Order.Id, ve.Id);
        SeedFaWorkStep(ctx, o2.Order.Id, ve.Id);

        var result = await ctrl.Index(ve.Id, workbenches: "Werkbank 2");

        var vm = (FaWorklistViewModel)((ViewResult)result).Model!;
        vm.Items.Should().ContainSingle().Which.OrderNumber.Should().Be("FA-002");
    }

    [Fact]
    public async Task Index_EmptyWorkbenchesParam_ShowsAll_IgnoresDefault()
    {
        var (ctx, ctrl, userMock) = Build();
        var wp1 = SeedWorkplace(ctx, "Werkbank 1");
        var wp2 = SeedWorkplace(ctx, "Werkbank 2");
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 1);
        var user = SeedUser(ctx, "vorbau1", defaultWorkStepId: ve.Id, defaultWorkbenches: "Werkbank 1");
        userMock.Setup(x => x.GetCurrentAppUserId()).Returns(user.Id);

        var o1 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var o2 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-002");
        o1.Order.ProductionWorkplaceId = wp1.Id;
        o2.Order.ProductionWorkplaceId = wp2.Id;
        ctx.SaveChanges();
        SeedFaWorkStep(ctx, o1.Order.Id, ve.Id);
        SeedFaWorkStep(ctx, o2.Order.Id, ve.Id);

        // ?workbenches= (present-empty) -> alle, Default greift NICHT.
        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?workbenches=");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index(ve.Id);

        var vm = (FaWorklistViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Index_WorkbenchFilter_ContainsMatchesPrefix()
    {
        var (ctx, ctrl, _) = Build();
        var wpA = SeedWorkplace(ctx, "WB-A");
        var wpA2 = SeedWorkplace(ctx, "WB-A2");
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 1);

        var o1 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var o2 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-002");
        o1.Order.ProductionWorkplaceId = wpA.Id;
        o2.Order.ProductionWorkplaceId = wpA2.Id;
        ctx.SaveChanges();
        SeedFaWorkStep(ctx, o1.Order.Id, ve.Id);
        SeedFaWorkStep(ctx, o2.Order.Id, ve.Id);

        // Token "WB-A" trifft "WB-A" UND "WB-A2" (Contains).
        var result = await ctrl.Index(ve.Id, workbenches: "WB-A");

        var vm = (FaWorklistViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(2);
    }
```

- [ ] **Step 10: Build + Tests**

Run: `dotnet build IdealAkeWms.Tests/IdealAkeWms.Tests.csproj`
Expected: Build succeeded (Rename überall durchgezogen, keine `DefaultWorkplaceId`-Reste).

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~FaWorklistControllerTests"`
Expected: PASS (bestehende umbenannte + 3 neue Tests grün).

> Falls noch `DefaultWorkplaceId`-Referenzen den Build brechen: `grep -rn "DefaultWorkplaceId" IdealAkeWms` und alle Treffer (außer in `Migrations/` — die historischen Designer/Snapshot-Dateien werden in Task 3 vom EF-Tool aktualisiert) auf `DefaultWorkbenches` umstellen.

- [ ] **Step 11: Commit**

```bash
git add IdealAkeWms/Models/User.cs IdealAkeWms/Data/ApplicationDbContext.cs IdealAkeWms/Models/ViewModels/ProfileViewModel.cs IdealAkeWms/Models/ViewModels/UserEditViewModel.cs IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs IdealAkeWms/Controllers/AccountController.cs IdealAkeWms/Controllers/UsersController.cs IdealAkeWms/Controllers/FaWorklistController.cs IdealAkeWms/Views/Account/Profile.cshtml IdealAkeWms/Views/Users/Edit.cshtml IdealAkeWms/Views/FaWorklist/Index.cshtml IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs
git commit -m "feat(faworklist): DefaultWorkplaceId->DefaultWorkbenches (Komma-Werkbank-Filter)"
```

---

## Task 3: Migration 75 + SQL/75 + FreshInstall

**Files:**
- Create: `IdealAkeWms/Migrations/<timestamp>_ReplaceUserDefaultWorkplaceWithWorkbenches.cs` (EF-generiert)
- Create: `SQL/75_ReplaceUserDefaultWorkplaceWithWorkbenches.sql`
- Modify: `SQL/00_FreshInstall.sql` (Users-Spalte Zeile 44; FK/Index-Block Zeilen 364-382; History-Insert)

- [ ] **Step 1: Migration generieren**

Run: `dotnet ef migrations add ReplaceUserDefaultWorkplaceWithWorkbenches --project IdealAkeWms/IdealAkeWms.csproj`
Expected: erzeugt `IdealAkeWms/Migrations/20260619HHMMSS_ReplaceUserDefaultWorkplaceWithWorkbenches.cs`. **Notiere die MigrationId** (Dateiname ohne `.cs`).

- [ ] **Step 2: Generierten Up/Down verifizieren**

Öffne die generierte `.cs`. `Up()` MUSS enthalten: `DropForeignKey("FK_Users_ProductionWorkplaces_DefaultWorkplaceId")`, `DropIndex("IX_Users_DefaultWorkplaceId")`, `DropColumn("DefaultWorkplaceId")`, `AddColumn<string>("DefaultWorkbenches", maxLength: 400, nullable: true)`. `Down()` das Gegenteil. Falls die Reihenfolge/Namen abweichen: belassen wie EF generiert (EF kennt die Constraint-Namen aus dem Snapshot). KEIN manuelles Editieren nötig.

> Falls `dotnet ef` einen Fehler wirft, weil noch `DefaultWorkplaceId`-Referenzen im Code sind: zuerst Task 2 vollständig abschließen.

- [ ] **Step 3: SQL/75 erstellen**

`SQL/75_ReplaceUserDefaultWorkplaceWithWorkbenches.sql` (`<MigrationId>` = ID aus Step 1):

```sql
-- =============================================
-- 75_ReplaceUserDefaultWorkplaceWithWorkbenches.sql (v1.23.0)
-- Ersetzt Users.DefaultWorkplaceId (FK -> ProductionWorkplaces) durch
-- Users.DefaultWorkbenches (NVARCHAR(400), kommasepariert). Idempotent.
-- Entspricht der EF-Migration <MigrationId>.
-- HINWEIS: destruktiv — bestehende DefaultWorkplaceId-Werte gehen verloren.
-- =============================================

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Users_ProductionWorkplaces_DefaultWorkplaceId')
BEGIN
    ALTER TABLE [dbo].[Users] DROP CONSTRAINT [FK_Users_ProductionWorkplaces_DefaultWorkplaceId];
    PRINT 'FK FK_Users_ProductionWorkplaces_DefaultWorkplaceId entfernt.';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_DefaultWorkplaceId' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    DROP INDEX [IX_Users_DefaultWorkplaceId] ON [dbo].[Users];
    PRINT 'Index IX_Users_DefaultWorkplaceId entfernt.';
END
GO

IF COL_LENGTH('dbo.Users', 'DefaultWorkplaceId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Users] DROP COLUMN [DefaultWorkplaceId];
    PRINT 'Spalte Users.DefaultWorkplaceId entfernt.';
END
GO

IF COL_LENGTH('dbo.Users', 'DefaultWorkbenches') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [DefaultWorkbenches] NVARCHAR(400) NULL;
    PRINT 'Spalte Users.DefaultWorkbenches erstellt.';
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '<MigrationId>')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('<MigrationId>', '10.0.2');
GO

PRINT '75_ReplaceUserDefaultWorkplaceWithWorkbenches abgeschlossen.';
GO
```

- [ ] **Step 4: FreshInstall anpassen**

In `SQL/00_FreshInstall.sql`:

(a) Users-Spalte (Zeile 44): `[DefaultWorkplaceId]        INT               NULL,` ersetzen durch:
```sql
    [DefaultWorkbenches]        NVARCHAR(400)     NULL,
```

(b) Den kompletten „8c3"-Block (Zeilen 364-382: Kommentar + `CREATE INDEX IX_Users_DefaultWorkplaceId` + `ADD CONSTRAINT FK_Users_ProductionWorkplaces_DefaultWorkplaceId`, inkl. der beiden `GO`) **ersatzlos löschen** (die neue Spalte braucht keinen FK/Index).

(c) History-Insert: direkt nach der `20260616070943_AddUserDefaultWorkplace`-Zeile (~2050-2051) eine neue Zeile ergänzen (`<MigrationId>` = ID aus Step 1). Die 71-Zeile bleibt erhalten:
```sql
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '<MigrationId>')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('<MigrationId>', '10.0.2');
```

- [ ] **Step 5: Build + Migrations-Check**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

Run: `dotnet ef migrations has-pending-model-changes --project IdealAkeWms/IdealAkeWms.csproj`
Expected: „No changes" (Model = Migration konsistent).

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Migrations/ SQL/75_ReplaceUserDefaultWorkplaceWithWorkbenches.sql SQL/00_FreshInstall.sql
git commit -m "feat(db): Migration 75 — DefaultWorkplaceId -> DefaultWorkbenches + SQL/75 + FreshInstall"
```

---

## Task 4: Bezeichnung 1 + 2 als Spalten (TDD)

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs` (`FaWorklistRow`)
- Modify: `IdealAkeWms/Controllers/FaWorklistController.cs` (Row-Erzeugung + `BuildColumnMap`)
- Modify: `IdealAkeWms/Views/FaWorklist/Index.cshtml` (th/td/column-config/columnCount)
- Test: `IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs`

- [ ] **Step 1: Failing test schreiben**

Am Ende der `FaWorklistControllerTests`-Klasse hinzufügen:

```csharp
    [Fact]
    public async Task Index_PopulatesAndFiltersDescriptions()
    {
        var (ctx, ctrl, _) = Build();
        var wp = SeedWorkplace(ctx, "Werkbank 1");
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 1);

        var o1 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var o2 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-002");
        o1.Order.ProductionWorkplaceId = wp.Id;
        o2.Order.ProductionWorkplaceId = wp.Id;
        o1.Order.Description1 = "Alpha"; o1.Order.Description2 = "Eins";
        o2.Order.Description1 = "Beta";  o2.Order.Description2 = "Zwei";
        ctx.SaveChanges();
        SeedFaWorkStep(ctx, o1.Order.Id, ve.Id);
        SeedFaWorkStep(ctx, o2.Order.Id, ve.Id);

        // Bezeichnung wird in die Rows uebernommen.
        var result = await ctrl.Index(ve.Id);
        var vm = (FaWorklistViewModel)((ViewResult)result).Model!;
        vm.Items.Single(i => i.OrderNumber == "FA-001").Description1.Should().Be("Alpha");
        vm.Items.Single(i => i.OrderNumber == "FA-001").Description2.Should().Be("Eins");

        // Spaltenfilter description1 wirkt.
        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_description1=Beta");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };
        var filtered = (FaWorklistViewModel)((ViewResult)(await ctrl.Index(ve.Id))).Model!;
        filtered.Items.Should().ContainSingle().Which.OrderNumber.Should().Be("FA-002");
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~Index_PopulatesAndFiltersDescriptions"`
Expected: FAIL — `FaWorklistRow.Description1`/`Description2` existieren nicht (Compile-Fehler).

- [ ] **Step 3: FaWorklistRow erweitern**

In `IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs` in `FaWorklistRow` nach `public string? ArticleNumber { get; set; }` einfügen:

```csharp
    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
```

- [ ] **Step 4: Controller — Row befüllen + ColumnMap**

In `IdealAkeWms/Controllers/FaWorklistController.cs` in der `new FaWorklistRow { ... }`-Initialisierung (Zeilen 176-189) nach `ArticleNumber = order.ArticleNumber,` einfügen:

```csharp
                Description1 = order.Description1,
                Description2 = order.Description2,
```

In `BuildColumnMap` (nach `["article-number"] = r => r.ArticleNumber,`) einfügen:

```csharp
            ["description1"] = r => r.Description1,
            ["description2"] = r => r.Description2,
```

- [ ] **Step 5: View — Spalten + column-config + columnCount**

In `IdealAkeWms/Views/FaWorklist/Index.cshtml`:

(a) `columnCount` (Zeile 17): `var columnCount = 7 + Model.AttributeColumns.Count + 1;` → `var columnCount = 9 + Model.AttributeColumns.Count + 1;`

(b) Im `<thead>` nach `<th data-filterable data-col-key="article-number">Artikelnummer</th>` zwei Zeilen einfügen:
```html
                    <th data-filterable data-col-key="description1">Bezeichnung 1</th>
                    <th data-filterable data-col-key="description2">Bezeichnung 2</th>
```

(c) Im `<tbody>`-`@foreach` nach `<td>@item.ArticleNumber</td>` zwei Zeilen einfügen:
```html
                            <td>@item.Description1</td>
                            <td>@item.Description2</td>
```

(d) Im `#column-config`-JSON nach der `article-number`-Zeile zwei Einträge einfügen:
```html
        { "key": "description1", "label": "Bezeichnung 1", "locked": false, "defaultWidth": null },
        { "key": "description2", "label": "Bezeichnung 2", "locked": false, "defaultWidth": null },
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~Index_PopulatesAndFiltersDescriptions"`
Expected: PASS.

- [ ] **Step 7: Build (Razor) + Commit**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

```bash
git add IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs IdealAkeWms/Controllers/FaWorklistController.cs IdealAkeWms/Views/FaWorklist/Index.cshtml IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs
git commit -m "feat(faworklist): Bezeichnung 1 + 2 als Spalten (Anzeige + Filter)"
```

---

## Task 5: Doku

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`, `CLAUDE.md`, `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md`

- [ ] **Step 1: Changelog-Bullet**

In `IdealAkeWms/Views/Help/Changelog.cshtml` in der **v1.23.0**-Karte am Ende der `<ul>` (vor `</ul>`) ergänzen:

```razor
                    <li><strong>FA-Abarbeitungsliste:</strong> Der Standard-Werkbank-Filter im Profil/
                        Benutzerstamm ist jetzt ein <em>komma-separiertes Textfeld</em> (mehrere
                        Werkbänke statt einer). Die Liste filtert per Enthält-Logik darauf vor und
                        zeigt zusätzlich <em>Bezeichnung 1</em> und <em>Bezeichnung 2</em> als Spalten.</li>
```

- [ ] **Step 2: CLAUDE.md — Fallstrick aktualisieren**

In `CLAUDE.md` den bestehenden Fallstrick „FA-Abarbeitung: zwei kombinierte Default-Filter (Werkbank UND Arbeitsgang, v1.22.0-Followup)" am Ende um folgenden Satz ergänzen (vor dem schließenden Satz/Punkt):

```
**Seit v1.23.0:** `User.DefaultWorkplaceId` (FK) wurde durch `User.DefaultWorkbenches` (komma-separierter String, **Migration 75**, destruktiv — alte FK-Werte verloren) ersetzt. Der FaWorklist-Filter ist ein Komma-Textfeld (`?workbenches=`) mit Enthält-/Komma-OR-Semantik (`WorkbenchFilter.Matches`, gleiche Semantik wie der `workbench`-Spaltenfilter, der zusätzlich per UND verfeinert). Param vorhanden&leer = alle, Param fehlt = `DefaultWorkbenches`. Profil + Benutzerstamm zeigen ein `<input list=...>` mit Datalist der Werkbank-Namen. Neu zusätzlich: Spalten `description1`/`description2` (Bezeichnung 1/2) in der Abarbeitungsliste.
```

- [ ] **Step 3: TESTSZENARIEN — neues Kapitel 43**

In `docs/TESTSZENARIEN.md` direkt vor der Zeile `*Ende des Dokuments. Stand: v1.23.0 (2026-06-18)*` (und dem `---` davor) einfügen:

```markdown
## Kapitel 43: FA-Abarbeitungsliste — Komma-Werkbank-Filter + Bezeichnung (v1.23.0)

**Vorbedingung:** `FaCompletionAktiv=true`. Mehrere Werkbänke (z. B. „WB-A", „WB-A2", „WB-B").
Offene FAs mit einem FA-Vorbau-AG auf verschiedenen Werkbänken, gefüllten Bezeichnungen.
Angemeldet als vorbau/admin-User.

### TS-43.1 Standard-Werkbänke im Profil (Textfeld)
1. Profil öffnen → Feld „Standard-Werkbänke (FA-Abarbeitungsliste, kommasepariert)".
   - **Erwartet:** Textfeld (kein Dropdown), Datalist schlägt vorhandene Werkbank-Namen vor.
2. „WB-A, WB-B" eintragen, speichern.
3. FA-Abarbeitungsliste öffnen (AG gewählt).
   - **Erwartet:** Werkbank-Feld vorbefüllt „WB-A, WB-B"; Liste zeigt nur FAs auf WB-A/WB-A2/WB-B
     (Enthält: „WB-A" trifft auch „WB-A2").

### TS-43.2 In-Listen-Filter override + leeren
1. In der Abarbeitungsliste ins Werkbank-Textfeld „WB-B" eintragen → Liste neu.
   - **Erwartet:** nur FAs deren Werkbank „WB-B" enthält.
2. Feld leeren, Liste neu.
   - **Erwartet:** ALLE Werkbänke (Default greift nicht, weil explizit geleert).

### TS-43.3 Bezeichnungs-Spalten
1. Abarbeitungsliste mit gewähltem AG.
   - **Erwartet:** Spalten „Bezeichnung 1" + „Bezeichnung 2" nach „Artikelnummer", gefüllt.
2. In „Bezeichnung 1" einen Spaltenfilter setzen.
   - **Erwartet:** Liste auf passende Zeilen reduziert.

### TS-43.4 Benutzerstamm (Admin)
1. Benutzer bearbeiten → „Standard-Werkbänke"-Textfeld pflegen/speichern.
   - **Erwartet:** wie Profil; Wert wird gespeichert.
```

- [ ] **Step 4: PROJECT_STATUS.md**

In `PROJECT_STATUS.md` im v1.23.0-Abschnitt einen Bullet ergänzen:

```markdown
- FA-Abarbeitungsliste: `User.DefaultWorkplaceId` (FK) → `User.DefaultWorkbenches`
  (komma-separierter Werkbank-Filter, Enthält-Semantik via `WorkbenchFilter`, **Migration 75**
  destruktiv) + Textfeld/Datalist in Profil/Benutzerstamm + neue Spalten Bezeichnung 1/2.
```

- [ ] **Step 5: Build + Commit**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

```bash
git add IdealAkeWms/Views/Help/Changelog.cshtml CLAUDE.md docs/TESTSZENARIEN.md PROJECT_STATUS.md
git commit -m "docs(v1.23.0): FA-Abarbeitung Komma-Werkbank-Filter + Bezeichnungs-Spalten"
```

---

## Task 6: Final-Check

**Files:** keine.

- [ ] **Step 1: Voller Build**

Run: `dotnet build IdealAkeWms.Tests/IdealAkeWms.Tests.csproj`
Expected: Build succeeded, 0 Errors.

- [ ] **Step 2: Volle Web-Testsuite**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj`
Expected: Alle grün (Baseline 754 + 12 WorkbenchFilter-Theory + 3 neue FaWorklist + 1 Description = ~770; tatsächliche Zahl prüfen, 1 Skip, keine Regression).

- [ ] **Step 3: Keine DefaultWorkplaceId-Reste + Migrations-Konsistenz**

Run: `dotnet ef migrations has-pending-model-changes --project IdealAkeWms/IdealAkeWms.csproj`
Expected: „No changes".

Run (Bash/PowerShell-Grep): nach `DefaultWorkplaceId` außerhalb von `IdealAkeWms/Migrations/` suchen.
Expected: keine Treffer (nur die historischen Migrations-/Snapshot-Dateien dürfen den alten Namen referenzieren, da sie Vergangenheit abbilden — der aktuelle Snapshot zeigt `DefaultWorkbenches`).

> **STOP — keine Merge-/Cleanup-Aktion.** Plan endet hier. Merge nach `main` + Worktree-/Branch-Cleanup nur auf ausdrückliche User-Freigabe.

---

## Self-Review (vom Plan-Autor)

**Spec-Coverage:**
- Spec §3.1 Datenmodell-Ersatz + Migration 75 → Task 2 (Model/EF) + Task 3 (Migration/SQL/FreshInstall). ✅
- Spec §3.2 User-Settings-UI (Textfeld+Datalist, ViewModels, POST) → Task 2 Steps 3-5,7. ✅
- Spec §3.3 FaWorklist-Filter (Signatur, Default-vs-Override, Contains, Spaltenfilter bleibt) → Task 1 (WorkbenchFilter) + Task 2 Step 6,8 + Task 2 Step 9 Tests. ✅
- Spec §4 Bezeichnung 1+2 → Task 4. ✅
- Spec §5 Tests → Task 1 (Helper) + Task 2 (Controller-Default/Override/empty/contains) + Task 4 (Description). ✅
- Spec §6 Doku → Task 5. ✅
- Spec §7 Risiken (destruktiv, Komma-im-Namen, zwei Filtermechanismen) → in CLAUDE.md-Fallstrick + TESTSZENARIEN dokumentiert (Task 5). ✅

**Placeholder-Scan:** Einzige `<MigrationId>`/`<timestamp>`-Marker in Task 3 sind EF-generierte Werte mit expliziter Beschaffungsanweisung (Projekt-Standard), kein Vagheits-Platzhalter. Sonst keine TBD/TODO.

**Typ-Konsistenz:** `User.DefaultWorkbenches` (string?), `ProfileViewModel.DefaultWorkbenches`, `UserEditViewModel.DefaultWorkbenches`, `FaWorklistViewModel.Workbenches` (string?), Action-Param `workbenches`, `WorkbenchFilter.Matches(name, filter)`, `FaWorklistRow.Description1/Description2` — durchgängig identisch in Model/VM/Controller/View/Tests. Test-Helper `SeedUser(..., string? defaultWorkbenches)`. Kein verbleibender `DefaultWorkplaceId`-Bezug außerhalb der historischen Migrationen (Final-Check Step 3 verifiziert das).
