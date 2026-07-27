# Vorbau-Stückliste-Button + Rolle `stock_read` — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** (1) FA-Liste an Rolle `vorbau` hängen + read-only-Stückliste-Button für vorbau; (2) neue additive Lese-Rolle `stock_read` für Bestände + Bewegungshistorie (read-only).

**Architecture:** ASP.NET Core 10 MVC, Rollen über `RoleKeys` + `ICurrentUserService`-Helper + `[Require…]`-TypeFilter. Item 1 = additive Filter-Erweiterung + View-Logik (kein DB-Change). Item 2 = neuer Read-Filter + Rollen-Seed (Migration 78 = reiner SQL-Rollen-Insert, kein Modell-Change).

**Tech Stack:** EF Core 10 (SQL Server), xUnit + FluentAssertions + Moq.

**Spec:** `docs/superpowers/specs/2026-07-07-vorbau-bom-button-stock-read-role-design.md`
**Worktree:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\glas-bestellung` (Branch `feature/glas-bestellung`, HEAD `d35a5e4`)

**Regeln:** Git im Worktree via **Bash** `cd <worktree> && git …` ODER `git -C <worktree>` (PowerShell-Tool cwd = Haupt-Repo!). Build: `dotnet build IdealAkeWms.slnx`. Web-Tests: `dotnet test IdealAkeWms.Tests`. Commit-Trailer `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. UI-Texte deutsch. Fold in v1.25.0 (kein Version-Bump).

---

## Datei-Landkarte

**Item 1:**
- `IdealAkeWms/Filters/RequirePickingOrTrackingOrLeitstandAccessAttribute.cs` — +vorbau
- `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs` — +`HasVorbauAccess`
- `IdealAkeWms/Controllers/ProductionOrdersController.cs` — VM-Init +`HasVorbauAccess`
- `IdealAkeWms/Views/ProductionOrders/Index.cshtml` — bedingter Button (Picking/Bom vs FaWorklist/Bom)
- `IdealAkeWms/Views/Shared/_Layout.cshtml` — `showFaList` +vorbau
- Test: `IdealAkeWms.Tests/Controllers/ProductionOrdersController*Tests.cs`

**Item 2:**
- `IdealAkeWms/Models/RoleKeys.cs` — +`StockRead`
- `IdealAkeWms/Services/ICurrentUserService.cs` + `CurrentUserService.cs` — +`CanAccessStockReadAsync`
- `IdealAkeWms/Filters/RequireStockReadAccessAttribute.cs` — NEU
- `IdealAkeWms/Controllers/StockOverviewController.cs` — Class-Attr swap
- `IdealAkeWms/Controllers/StockMovementsController.cs` — nur `Index`-Attr swap
- `IdealAkeWms/Program.cs` — `defaultRoles` +Eintrag
- `IdealAkeWms/Migrations/<ts>_AddStockReadRole.cs` + `SQL/78_AddStockReadRole.sql` + `SQL/00_FreshInstall.sql`
- `IdealAkeWms/Views/Shared/_Layout.cshtml` — Lager-Menü-Gating
- `IdealAkeWms/Views/Users/RoleOverview.cshtml` — neue Zeile
- Doku: `CLAUDE.md`, `IdealAkeWms/Views/Help/Changelog.cshtml`, `docs/TESTSZENARIEN.md`
- Test: `IdealAkeWms.Tests/Services/CurrentUserServiceRoleTests.cs`

---

### Task 0: Pre-Flight

- [ ] **Step 0.1:** `cd` in Worktree; `git status --short` leer; `git log --oneline -1` = `d35a5e4`.
- [ ] **Step 0.2:** `dotnet build IdealAkeWms.slnx` → 0 Fehler.
- [ ] **Step 0.3:** `dotnet test IdealAkeWms.Tests --nologo` → grün (Baseline-Zahl notieren, erwartet 839/1 Skip). `dotnet test IDEALAKEWMSService.Tests --nologo` → 123.

---

### Task 1: Item 1 — FA-Liste an vorbau + read-only-Stückliste-Button

**Files:** siehe Item-1-Landkarte.

- [ ] **Step 1.1: Filter erweitern** — `RequirePickingOrTrackingOrLeitstandAccessAttribute.cs`, in `RequirePickingOrTrackingOrLeitstandAccessFilter.OnActionExecutionAsync` die Bedingung (Zeilen 23-25) ersetzen:
```csharp
        if (!await _currentUserService.CanPickAsync()
            && !await _currentUserService.CanViewTrackingAsync()
            && !await _currentUserService.CanManagePickingReleaseAsync()
            && !await _currentUserService.HasVorbauAccessAsync())
```
XML-/Kommentar am Attribut ergänzen: „seit v1.25.0 auch vorbau (FA-Liste + read-only Stückliste)". Filter-Name unverändert (nur an ProductionOrdersController verwendet).

- [ ] **Step 1.2: VM-Property** — `ProductionOrderListViewModel.cs`, nach `public bool CanPick { get; set; }` (Zeile 13):
```csharp
    /// <summary>vorbau-Zugriff (read-only Stueckliste-Button in der FA-Liste, v1.25.0).</summary>
    public bool HasVorbauAccess { get; set; }
```

- [ ] **Step 1.3: Controller setzt Flag** — `ProductionOrdersController.cs`, im VM-Initializer (nach `CanPick = await _currentUserService.CanPickAsync(),`, Zeile 169):
```csharp
            HasVorbauAccess = await _currentUserService.HasVorbauAccessAsync(),
```

- [ ] **Step 1.4: Layout-Menü** — `_Layout.cshtml`, Zeile 48: `var showFaList = canPick || canViewTracking || canManagePickingRelease;` →
```csharp
                            var showFaList = canPick || canViewTracking || canManagePickingRelease || hasVorbauAccess;
```
(`hasVorbauAccess` ist Zeile 47 bereits berechnet.)

- [ ] **Step 1.5: View-Button** — `Views/ProductionOrders/Index.cshtml`. Die aktuelle Struktur: Actions-`<th>` in `@if (Model.CanPick)`, und in der Zeile die Actions-`<td>` in `@if (Model.CanPick)` mit Link auf `Picking/Bom`. Umbauen:
  1. Actions-`<th>` (aktuell `@if (Model.CanPick) { <th …data-col-key="actions"></th> }`): Bedingung → `@if (Model.CanPick || Model.HasVorbauAccess)`.
  2. Actions-`<td>`: von `@if (Model.CanPick) { <td…>…Picking/Bom…</td> }` auf:
```razor
@if (Model.CanPick || Model.HasVorbauAccess)
{
    <td class="text-nowrap">
        @if (!string.IsNullOrEmpty(item.ArticleNumber))
        {
            @if (Model.CanPick)
            {
                <a asp-controller="Picking" asp-action="Bom" asp-route-id="@item.Id" class="btn btn-sm btn-outline-primary me-1" title="Stückliste">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><path d="M14 4.5V14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h5.5zm-3 0A1.5 1.5 0 0 1 9.5 3V1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V4.5z"/></svg>
                </a>
            }
            else
            {
                <a asp-controller="FaWorklist" asp-action="Bom" asp-route-id="@item.Id" class="btn btn-sm btn-outline-primary me-1" title="Stückliste">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><path d="M14 4.5V14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h5.5zm-3 0A1.5 1.5 0 0 1 9.5 3V1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V4.5z"/></svg>
                </a>
            }
        }
    </td>
}
```
WICHTIG: Das GENAUE `<svg>`-Markup (path-d) aus dem bestehenden Button übernehmen (oben ist ein Platzhalter-Icon — beim Umbau das vorhandene Icon-Markup der Datei 1:1 kopieren, NICHT ersetzen). Der einzige Unterschied zwischen den zwei `<a>` ist `asp-controller` (Picking vs FaWorklist). Falls die bestehende Actions-`<td>` weitere Buttons enthält (nur bei CanPick sinnvoll, z. B. Kommissionieren), diese im `@if (Model.CanPick)`-Zweig belassen und für vorbau NUR den Stückliste-Button zeigen.

- [ ] **Step 1.6: Test** — in der ProductionOrdersController-Testdatei (suchen: `ls IdealAkeWms.Tests/Controllers/ | grep -i productionorder`; Setup-Muster spiegeln, `Mock<ICurrentUserService>`): Test `Index_SetztHasVorbauAccess` — Mock `HasVorbauAccessAsync()` → true, `Index(...)` aufrufen, VM aus `ViewResult.Model` casten, `vm.HasVorbauAccess.Should().BeTrue()`. Zweiter Fall false → false. (Falls die Datei/das Setup nicht existiert oder der Controller viele Repos braucht, die im Test aufwändig zu mocken sind: minimalen Test bauen ODER im Report als Manual-UAT dokumentieren — nicht erzwingen.)

- [ ] **Step 1.7: Verifizieren** — `dotnet build IdealAkeWms.slnx` 0 Fehler; `dotnet test IdealAkeWms.Tests --nologo` grün.
- [ ] **Step 1.8: Commit** — `fix(fa-liste): FA-Liste + read-only Stueckliste-Button fuer Rolle vorbau`

---

### Task 2: Item 2 — RoleKeys + Service-Helper + Read-Filter (TDD)

**Files:** `RoleKeys.cs`, `ICurrentUserService.cs`, `CurrentUserService.cs`, NEU `Filters/RequireStockReadAccessAttribute.cs`, Test `CurrentUserServiceRoleTests.cs`.

- [ ] **Step 2.1: Failing Tests** — in `IdealAkeWms.Tests/Services/CurrentUserServiceRoleTests.cs` (Muster der bestehenden `CanAccessStock`/`HasMasterDataReadAccess`-Tests spiegeln): `CanAccessStockReadAsync` — true für `admin`, `stock`, `stock_keyuser`, `picking`, `stock_read`; false für nur-`tracking` und keine Rolle. Nutzt `RoleKeys.StockRead` (Konstante entsteht in 2.3 → Compile-Fail = erwarteter Red).
- [ ] **Step 2.2:** `dotnet test IdealAkeWms.Tests --filter CanAccessStockRead --nologo` → FAIL (Compile).
- [ ] **Step 2.3: Implementieren:**
  - `RoleKeys.cs` nach `Glasbestellung`: `public const string StockRead = "stock_read";`
  - `ICurrentUserService.cs` nach `CanAccessStockAsync();`: `Task<bool> CanAccessStockReadAsync();`
  - `CurrentUserService.cs` nach `CanAccessStockAsync`-Impl:
```csharp
    // Additive Lese-Rolle: alle bisherigen Stock-Zugriffsrollen PLUS stock_read (v1.25.0).
    public async Task<bool> CanAccessStockReadAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Stock, RoleKeys.StockKeyUser, RoleKeys.Picking, RoleKeys.StockRead);
```
  Prüfe: JEDE `ICurrentUserService`-Implementierung (grep `: ICurrentUserService`) — auch Test-Fakes (`FakeCurrentUserService`) brauchen die neue Methode (Default `Task.FromResult(true)` wie Nachbarn).
  - NEU `Filters/RequireStockReadAccessAttribute.cs` (Muster = `RequireMasterDataReadAccessAttribute.cs`):
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

/// <summary>
/// Erfordert Lese-Zugriff auf Lagerbestand (Bestände + Bewegungshistorie).
/// admin/stock/stock_keyuser/picking impliziert es; zusaetzlich die reine
/// Lese-Rolle stock_read (v1.25.0). Schreib-Actions verschaerfen mit
/// [RequireStockAccess]/[RequireStockKeyUserAccess].
/// </summary>
public class RequireStockReadAccessAttribute : TypeFilterAttribute
{
    public RequireStockReadAccessAttribute() : base(typeof(RequireStockReadAccessFilter)) { }
}

public class RequireStockReadAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireStockReadAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanAccessStockReadAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }
}
```
- [ ] **Step 2.4:** Tests grün; `dotnet build IdealAkeWms.slnx` 0 Fehler; voller `dotnet test IdealAkeWms.Tests` grün.
- [ ] **Step 2.5: Commit** — `feat(auth): Rolle stock_read + CanAccessStockReadAsync + RequireStockReadAccess-Filter (TDD)`

---

### Task 3: Item 2 — Controller umhängen

**Files:** `StockOverviewController.cs`, `StockMovementsController.cs`.

- [ ] **Step 3.1:** `StockOverviewController.cs`: Class-Level `[RequireStockAccess]` → `[RequireStockReadAccess]` (ganzer Controller read-only).
- [ ] **Step 3.2:** `StockMovementsController.cs`: NUR die `Index`-Action (Zeile 39) `[RequireStockAccess]` → `[RequireStockReadAccess]`. ALLE anderen Attribute UNVERÄNDERT: `Inbound` (86/100), `Outbound` (155/167), `Transfer` (243/254) bleiben `[RequireStockAccess]`; `OutboundAll`/`OutboundAllConfirm`/`LocationTransfer`/`LocationTransferConfirm` (333/364/409/435) bleiben `[RequireStockKeyUserAccess]`. (Verifiziere per grep, dass GENAU eine Attribut-Zeile geändert wurde.)
- [ ] **Step 3.3:** Build 0 Fehler; `dotnet test IdealAkeWms.Tests` grün.
- [ ] **Step 3.4: Commit** — `feat(stock): StockOverview + StockMovements.Index auf RequireStockReadAccess (stock_read read-only)`

---

### Task 4: Item 2 — Rollen-Seed + Migration 78 + SQL/78 + FreshInstall

**Files:** `Program.cs`, neue Migration, `SQL/78_AddStockReadRole.sql`, `SQL/00_FreshInstall.sql`.

- [ ] **Step 4.1: Program.cs Seed** — im `defaultRoles`-Array (nach `StockKeyUser`-Zeile, SortOrder 40) einfügen:
```csharp
        (RoleKeys.StockRead, "Lagerbestand-Ansicht", "Nur-Lesen-Zugriff auf Bestände und Bewegungshistorie", 35),
```
- [ ] **Step 4.2: EF-Migration** — `dotnet ef migrations add AddStockReadRole --project IdealAkeWms`. Da kein Modell-Change: Up/Down sind leer generiert. Fülle sie (Muster = `20260619063919_AddLagerbestellungRole.cs`):
```csharp
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'stock_read')
BEGIN
    INSERT INTO Roles ([Key], [Name], [Description], [IsSystem], [SortOrder],
                       [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('stock_read', 'Lagerbestand-Ansicht',
            'Nur-Lesen-Zugriff auf Bestände und Bewegungshistorie.',
            1, 35,
            SYSDATETIME(), 'system', 'system')
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM Roles WHERE [Key] = 'stock_read'");
        }
```
Exakten Migrations-Timestamp-Dateinamen notieren (für SQL/78 + FreshInstall). `dotnet ef migrations has-pending-model-changes --project IdealAkeWms` → „No changes…" (reiner Sql-Insert, kein Model-Change).
- [ ] **Step 4.3: SQL/78** — `SQL/78_AddStockReadRole.sql` (Muster = `SQL/74`, `<TS>` = echter Timestamp):
```sql
-- ============================================================================
-- Migration 78: AddStockReadRole (v1.25.0)
-- ============================================================================
-- Rolle 'stock_read' (Nur-Lesen: Bestände + Bewegungshistorie). Idempotent.
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'stock_read')
    BEGIN
        INSERT INTO Roles ([Key], [Name], [Description], [IsSystem], [SortOrder],
                           [CreatedAt], [CreatedBy], [CreatedByWindows])
        VALUES ('stock_read', 'Lagerbestand-Ansicht',
                'Nur-Lesen-Zugriff auf Bestände und Bewegungshistorie.',
                1, 35,
                SYSDATETIME(), 'system', 'system');
        PRINT 'Rolle stock_read angelegt.';
    END
    IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '<TS>_AddStockReadRole')
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('<TS>_AddStockReadRole', '10.0.2');
    END
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
```
- [ ] **Step 4.4: FreshInstall** — `SQL/00_FreshInstall.sql`: (a) nach dem `glasbestellung`-Rollen-Block einen analogen `stock_read`-Block (GETDATE()-Stil, SortOrder 35, Kommentar `-- Rolle 'stock_read' (Nur-Lesen Bestände+Bewegungen, v1.25.0)`); (b) im `__EFMigrationsHistory`-Block den Insert für `<TS>_AddStockReadRole` (Format der Nachbarzeilen spiegeln, chronologisch ans Ende nach `20260706074119_AddWarehouseRequisitionTypeAndGlasRole`).
- [ ] **Step 4.5: Verifizieren** — Build 0 Fehler; `has-pending-model-changes` leer; `dotnet test IdealAkeWms.Tests` grün. Grep: `grep -c "stock_read" SQL/00_FreshInstall.sql` ≥ 2; Timestamp in Migration == SQL/78 == FreshInstall-History.
- [ ] **Step 4.6: Commit** — `feat(auth): Rolle stock_read seeden (Migration 78 + SQL/78 + FreshInstall)`

---

### Task 5: Item 2 — Layout-Menü + RoleOverview + Doku

**Files:** `_Layout.cshtml`, `RoleOverview.cshtml`, `CLAUDE.md`, `Changelog.cshtml`, `docs/TESTSZENARIEN.md`.

- [ ] **Step 5.1: Layout-Menü** — `_Layout.cshtml`:
  - Bei den Variablen (nahe `canAccessStock`): `var canAccessStockRead = await CurrentUserService.CanAccessStockReadAsync();`
  - Lager-Dropdown-Gate `@if (canAccessStock || canPick)` → `@if (canAccessStock || canPick || canAccessStockRead)`.
  - Die Buchungs-Einträge (Einbuchung/Ausbuchung/Umbuchung) in `@if (canAccessStock || canPick) { … }` schachteln, sodass sie für pure-stock_read NICHT erscheinen. „Bestände" + „Bewegungshistorie" bleiben immer sichtbar (im Dropdown, das jetzt auch für stock_read offen ist). Die Lagerplatz-Sonderaktionen bleiben zusätzlich hinter `CanTransferStockAsync()` (unverändert). Ergebnis: pure-stock_read sieht NUR „Bestände" + „Bewegungshistorie".
- [ ] **Step 5.2: RoleOverview** — `Views/Users/RoleOverview.cshtml`, neue Zeile nach der `stock`-Zeile (Muster spiegeln):
```html
<tr>
    <td><code>stock_read</code></td>
    <td>Lagerbestand-Ansicht</td>
    <td>Nur-Lesen-Zugriff auf Bestände und Bewegungshistorie.</td>
    <td>
        <ul class="mb-0 small">
            <li>Bestandsuebersicht (<code>/StockOverview</code>, read-only)</li>
            <li>Bewegungshistorie (<code>/StockMovements</code> Index, read-only)</li>
            <li>KEIN Zugriff auf Ein-/Aus-/Umbuchung, Lagerplatz-Sonderaktionen, Lager-Worklist, Bedarfsmeldungen</li>
        </ul>
    </td>
</tr>
```
- [ ] **Step 5.3: CLAUDE.md** — (a) Rollen-Tabelle: Zeile `stock_read`; (b) Zugriffsschutz-Tabelle: neue Zeile `[RequireStockReadAccess]` | admin, stock, stock_keyuser, picking, stock_read | StockOverviewController (class), StockMovementsController.Index (v1.25.0). Bestehende `[RequireStockAccess]`-Zeile: Hinweis ergänzen, dass StockOverview + StockMovements.Index jetzt `[RequireStockReadAccess]` tragen.
- [ ] **Step 5.4: Changelog** — in der v1.25.0-Card ein `<li>`: „**Neue Rolle „Lagerbestand-Ansicht":** Nur-Lese-Zugriff auf Bestände und Bewegungshistorie (ohne Buchungsrechte)." + ein `<li>` für Item 1: „**FA-Liste für Vorbau:** Vorbau-Mitarbeiter sehen die FA-Liste inkl. read-only Stückliste-Button."
- [ ] **Step 5.5: TESTSZENARIEN** — neues Kapitel (Nummer fortführen, aktuell 47 → 48): TS für (a) pure-stock_read sieht Bestände+Bewegungshistorie read-only, KEINE Buchungs-Menüs, Schreib-URLs → AccessDenied; admin/stock unverändert. (b) vorbau-User sieht FA-Liste + Stückliste-Button → öffnet read-only FaWorklist/Bom (keine Kommissionier-Controls); Picker sieht weiterhin Picking/Bom.
- [ ] **Step 5.6:** Build 0 Fehler. **Commit** — `feat(ui)+docs: stock_read Menue/RoleOverview + CLAUDE/Changelog/TESTSZENARIEN (v1.25.0)`

---

### Task 6: Final-Check + Reviews

- [ ] **Step 6.1:** `dotnet build IdealAkeWms.slnx` 0 Fehler; `dotnet test IdealAkeWms.Tests` + `IDEALAKEWMSService.Tests` alle grün; `has-pending-model-changes` leer.
- [ ] **Step 6.2:** Konsistenz-Greps: genau EINE Attribut-Zeile in StockMovements geändert; `stock_read` in RoleKeys/Program/FreshInstall/SQL78 vorhanden; Migrations-Timestamp konsistent.
- [ ] **Step 6.3:** Spec- + Quality-Review über den Task-Diff je Item; Findings fixen.

---

### Task 7: PAUSE — User-Test (NICHT autonom mergen)

- [ ] **Step 7.1:** Zusammenfassung an User; auf Abnahme warten (vorbau-User: FA-Liste + Stückliste; stock_read-User: nur Bestände+Bewegungen). Kein Merge/Cleanup ohne Freigabe.

---

## Self-Review (beim Planen geprüft)
- **Spec-Abdeckung:** Item 1 (Filter T1.1, Menü T1.4, VM+Controller T1.2/1.3, Button T1.5, Test T1.6) + Item 2 (RoleKeys/Service/Filter T2, Controller T3, Seed/Migration T4, UI/Doku T5). Vollständig.
- **Typ-Konsistenz:** `HasVorbauAccess` (bool), `CanAccessStockReadAsync()` (Task<bool>), `RequireStockReadAccessAttribute`. Migration 78 = reiner Sql-Insert (kein Model-Change → has-pending bleibt leer).
- **Kein Privileg-Leak:** vorbau → read-only FaWorklist/Bom (nicht Picking/Bom); stock_read → nur Index-Actions, alle Schreib-Attribute unverändert.
