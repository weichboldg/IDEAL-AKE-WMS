# FA-Abarbeitungsliste: Arbeitsgang-Filter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Die FA-Abarbeitungsliste filtert kuenftig nach **Arbeitsgang** (statt Werkbank); die Werkbank wird zur Info-Spalte. Jeder User hinterlegt seinen Standard-Arbeitsgang im Profil.

**Architecture:** Neues nullable `User.DefaultWorkStepId` (FK WorkStep). `FaWorklistController` filtert auf einen gewaehlten WorkStep: zeigt alle offenen FAs mit aktivem (IsRemoved=0), nicht-erledigtem FaWorkStep dieses AGs — ueber alle Werkbaenke. Spalten: Werkbank + EIN Erledigt-Haken (der gewaehlte AG) + Merkmal-Spalten dieses AGs. Das Werkbank-AG-Mapping (ProductionWorkplaceWorkSteps) bleibt erhalten, wird aber von der Liste nicht mehr genutzt.

**Tech Stack:** ASP.NET Core 10 MVC + EF Core 10 (SQL Server), xUnit + FluentAssertions + Moq + EF InMemory.

**Rahmen:** Worktree `.claude/worktrees/missingparts-include-pd`, Branch `bugfix/missingparts-include-pd`, HEAD `9730c24`. Faltet in v1.22.0 — KEIN Versions-Bump. Migration **70**. Baseline: 696 Web (1 skipped) + 104 Service gruen. Einzel-Select (ein AG), per-User-Default.

---

### Task 0: Pre-Flight

- [ ] `git log --oneline -1` → `9730c24` (oder neuer), clean. `dotnet build IdealAkeWms.slnx` + `dotnet test IdealAkeWms.slnx --no-build` → 696 Web (1 skipped) + 104 Service. Zahlen notieren.

---

### Task 1: User.DefaultWorkStepId + Migration 70

**Files:**
- Modify: `IdealAkeWms/Models/User.cs`
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs` (User-Block: FK)
- Migration: `dotnet ef migrations add AddUserDefaultWorkStep`
- Create: `SQL/70_AddUserDefaultWorkStep.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1:** `User.cs` nach `DefaultPageSize` (Zeile 64):

```csharp
    /// <summary>Vorausgewaehlter Arbeitsgang in der FA-Abarbeitungsliste (NULL = keiner).</summary>
    [Display(Name = "Standard-Arbeitsgang (FA-Abarbeitungsliste)")]
    public int? DefaultWorkStepId { get; set; }
    public WorkStep? DefaultWorkStep { get; set; }
```

- [ ] **Step 2:** `ApplicationDbContext.cs` im `modelBuilder.Entity<User>(...)`-Block:

```csharp
            entity.HasOne(e => e.DefaultWorkStep)
                .WithMany()
                .HasForeignKey(e => e.DefaultWorkStepId)
                .OnDelete(DeleteBehavior.SetNull);
```

- [ ] **Step 3:** `dotnet ef migrations add AddUserDefaultWorkStep --project IdealAkeWms`. `Down()` von EF generiert. Pruefen: `dotnet ef migrations has-pending-model-changes --project IdealAkeWms` → keine.

- [ ] **Step 4:** `SQL/70_AddUserDefaultWorkStep.sql` (idempotent, Muster `SQL/69`): Spalte via `IF COL_LENGTH('dbo.Users','DefaultWorkStepId') IS NULL ALTER TABLE [dbo].[Users] ADD [DefaultWorkStepId] INT NULL;` (eigener Batch), FK via `IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Users_DefaultWorkStep') ALTER TABLE [dbo].[Users] ADD CONSTRAINT [FK_Users_DefaultWorkStep] FOREIGN KEY ([DefaultWorkStepId]) REFERENCES [dbo].[WorkSteps]([Id]) ON DELETE SET NULL;` (eigener Batch), `__EFMigrationsHistory`-INSERT mit der generierten MigrationId (IF NOT EXISTS, ProductVersion 10.0.2).

- [ ] **Step 5:** `SQL/00_FreshInstall.sql`: im `Users`-CREATE-TABLE die Spalte `[DefaultWorkStepId] INT NULL` ergaenzen; FK-Constraint im passenden Block ergaenzen (ACHTUNG: WorkSteps-Tabelle muss vor dem FK existieren — falls Users vor WorkSteps angelegt wird, FK als separates `ALTER TABLE ... ADD CONSTRAINT` am Ende nach beiden Tabellen, Muster anderer Cross-Tabellen-FKs im Skript); MigrationId im History-Block. **Beide Stellen.**

- [ ] **Step 6: Build + Vollsuite + Commit:**

```bash
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
dotnet test IdealAkeWms.slnx --no-build 2>&1 | tail -4
git add -A
git commit -m "feat(db): User.DefaultWorkStepId + Migration 70"
```

---

### Task 2: Profil + Benutzerstamm UI (TDD)

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/ProfileViewModel.cs` + `UserEditViewModel.cs`
- Modify: `IdealAkeWms/Controllers/AccountController.cs` (Profile GET/POST) + `UsersController.cs` (Edit GET/POST)
- Modify: `IdealAkeWms/Views/Account/Profile.cshtml` + `IdealAkeWms/Views/Users/Edit.cshtml`
- Test: `IdealAkeWms.Tests/Controllers/AccountControllerTests.cs` (+1) bzw. `UsersControllerTests.cs` (+1)

- [ ] **Step 1: ViewModels** — in beiden ein `int? DefaultWorkStepId { get; set; }` ergaenzen + `List<WorkStep> AvailableWorkSteps { get; set; } = new();` (fuer das Dropdown). Muster: wie `DefaultPageSize` dort schon eingebunden ist.

- [ ] **Step 2: Failing Test** — z.B. `Profile_Post_SavesDefaultWorkStep`: POST mit `DefaultWorkStepId = 10` → `User.DefaultWorkStepId == 10` in der DB (Setup-Muster der Bestandsklasse, echte Repos/InMemory). FAIL.

- [ ] **Step 3: Controller** — Profile GET + Users/Edit GET laden `AvailableWorkSteps` (aktive WorkSteps via `IWorkStepRepository.GetActiveAsync()`) + aktuellen `DefaultWorkStepId`; die POST-Actions uebernehmen `DefaultWorkStepId` auf den User (genau dort wo heute `DefaultPageSize` gesetzt wird; null = "(keiner)"). `IWorkStepRepository` ggf. in den Controller injizieren (DI global vorhanden).

- [ ] **Step 4: Views** — in `Profile.cshtml` + `Users/Edit.cshtml` neben dem DefaultPageSize-Feld ein Dropdown:

```html
<div class="mb-3">
    <label asp-for="DefaultWorkStepId" class="form-label">Standard-Arbeitsgang (FA-Abarbeitungsliste)</label>
    <select asp-for="DefaultWorkStepId" class="form-select">
        <option value="">(keiner)</option>
        @foreach (var ws in Model.AvailableWorkSteps)
        {
            <option value="@ws.Id">@ws.Code — @ws.Name</option>
        }
    </select>
</div>
```

- [ ] **Step 5: PASS + Vollsuite + Commit:**

```bash
git add -A
git commit -m "feat(profile): Standard-Arbeitsgang je User (Profil + Benutzerstamm)"
```

---

### Task 3: FaWorklist auf Arbeitsgang-Filter (TDD)

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs`
- Modify: `IdealAkeWms/Controllers/FaWorklistController.cs` (Index)
- Modify: `IdealAkeWms/Views/FaWorklist/Index.cshtml`
- Test: `IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs`

- [ ] **Step 1: ViewModel umbauen** — `FaWorklistViewModel.cs`:

```csharp
public class FaWorklistViewModel
{
    public int? SelectedWorkStepId { get; set; }
    public List<WorkStep> AvailableWorkSteps { get; set; } = new();
    public WorkStep? SelectedWorkStep { get; set; }              // Header der Erledigt-Spalte
    public List<FaAttributeDefinition> AttributeColumns { get; set; } = new(); // Merkmale des gewaehlten AG
    public bool ShowDone { get; set; }
    public List<FaWorklistRow> Items { get; set; } = new();
    public Dictionary<string, List<EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();
    public PaginationState Pagination { get; set; } = new();
}

public class FaWorklistRow
{
    public int ProductionOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public decimal Quantity { get; set; }
    public string? WorkplaceName { get; set; }                  // NEU: Werkbank als Info-Spalte
    public DateTime? VorkommissionierTermin { get; set; }
    public DateTime? KommissionierTermin { get; set; }
    public DateTime? ProductionDate { get; set; }
    public Dictionary<int, string?> AttributeValues { get; set; } = new();
    public FaWorklistCell? WorkStepCell { get; set; }           // genau EIN AG (der gewaehlte)
}

public class FaWorklistCell
{
    public int FaWorkStepId { get; set; }
    public bool IsCompleted { get; set; }
}
```

- [ ] **Step 2: Controller-Index neu** — `FaWorklistController.Index(int? workStepId, bool showDone = false, int page = 1, int? pageSize = null)`:
  1. `AvailableWorkSteps = await _workStepRepository.GetActiveAsync()`.
  2. **Default:** wenn `workStepId == null`, aus dem aktuellen User laden: `DefaultWorkStepId` (via `ICurrentUserService.GetCurrentAppUserId()` → `IUserRepository.GetByIdAsync` → `DefaultWorkStepId`). Wenn auch das null → nur Dropdown rendern, leere Liste (wie bisher ohne Werkbank).
  3. `SelectedWorkStep` = der gewaehlte WorkStep (aus AvailableWorkSteps).
  4. `AttributeColumns = await _faAttributeRepository.GetActiveForWorkStepsAsync(new List<int>{ workStepId.Value })`.
  5. **FAs laden:** offene FAs (`!IsDone && !(PickingStatus != null && PickingStatus.IsDonePicking)`) — KEIN Werkbank-Filter mehr — die einen aktiven (IsRemoved=0) FaWorkStep fuer `workStepId` haben. Effizient: erst `FaWorkSteps.Where(f => f.WorkStepId == workStepId && !f.IsRemoved).Select(f => f.ProductionOrderId)` → ProductionOrders dazu laden (Include ProductionWorkplace + PickingStatus). Pro FA den FaWorkStep des gewaehlten AGs holen (Id + IsCompleted).
  6. **showDone:** wenn `!showDone` und der FaWorkStep des AGs `IsCompleted` → Zeile ueberspringen.
  7. **Row mappen:** WorkplaceName = `order.ProductionWorkplace?.Name`; WorkStepCell = `new FaWorklistCell { FaWorkStepId = fws.Id, IsCompleted = fws.IsCompleted }`; Termine wie bisher (BusinessDayService); AttributeValues wie bisher (`GetValuesByProductionOrderIdAsync` + FormatAttributeValue). KEIN OrphanWorkStepCount mehr.
  8. **ColumnMap** (BuildColumnMap): `order-number`, `article-number`, `quantity`, `workbench` (= WorkplaceName), `bg-date`, `picking-date`, `production-date` + `attr-{DefinitionId}`. Apply VOR Pagination (Termine vorher berechnen — Fallstrick beibehalten).
  9. enaio-Links wie bisher (nach Pagination).
  - Werkbank-Mapping-Logik (`GetWorkStepIdsAsync`, `mappedIdSet`) + `OrphanWorkStepCount` ENTFERNEN.

- [ ] **Step 3: Failing Tests** (Setup-Muster der Bestandsklasse, InMemory):
  - `Index_FiltersByWorkStep_AcrossWorkplaces`: 2 FAs auf verschiedenen Werkbaenken, beide mit aktivem VE-FaWorkStep, eine zusaetzlich VL → Filter workStepId=VE → beide erscheinen, WorkplaceName gesetzt, WorkStepCell != null.
  - `Index_HidesWorkDone_UnlessShowDone`: FaWorkStep des AGs IsCompleted=true → ohne showDone weg, mit showDone da.
  - `Index_UsesUserDefaultWorkStep_WhenNoParam`: User mit DefaultWorkStepId=VE, Aufruf ohne workStepId → SelectedWorkStepId == VE.
  - `Index_ColumnFilter_FiltersAcrossAllRows`: Filter auf order-number.

- [ ] **Step 4: FAIL → Implementierung → PASS.**

- [ ] **Step 5: View** — `FaWorklist/Index.cshtml`:
  - Filter-Card: Werkbank-Dropdown → **Arbeitsgang-Dropdown** (`SelectedWorkStepId`, Optionen `AvailableWorkSteps`, onchange-Submit) + "Erledigte anzeigen"-Checkbox.
  - Tabellenspalten: FA Nr (Link auf Bom), **Werkbank** (NEU, `data-col-key="workbench"` filterbar), Artikelnummer, Stk, BG/Komm/Fert-Termin, Merkmal-Spalten, **eine** Erledigt-Spalte (Header = `SelectedWorkStep.Code SelectedWorkStep.Name`). Zelle: Checkbox `class="worklist-complete" data-fa-work-step-id="@item.WorkStepCell?.FaWorkStepId"` (nur wenn WorkStepCell != null). Orphan-Badge entfernen.
  - Inline-JS `.worklist-complete` → `/api/fa-work-steps/toggle-completed` UNVERAENDERT (existiert schon).
  - Pflicht-Pattern: Pagination, Server-Spaltenfilter, Datums-th `data-date-filter`, Scripts-Section.
  - Ohne gewaehlten AG: Filter-Card + Hinweis, keine Tabelle.

- [ ] **Step 6: Vollsuite + Commit:**

```bash
git add -A
git commit -m "feat(worklist): FA-Abarbeitungsliste filtert nach Arbeitsgang (Werkbank als Spalte)"
```

---

### Task 4: PrintBom-Zugriff fuer fa_completion (Folge aus Fix B)

**Files:**
- Modify: `IdealAkeWms/Filters/` (Composite-Filter) + `IdealAkeWms/Controllers/PickingController.cs` (PrintBom-Attribut)

- [ ] **Step 1:** `PickingController.PrintBom` traegt `[RequirePickingOrVorbauAccess]`. Da die read-only BOM jetzt auch aus FA-Vervollstaendigung (Rolle `fa_completion`) erreichbar ist, den Filter um fa_completion erweitern: neues `RequirePickingOrVorbauOrFaCompletionAccessAttribute` (Muster der bestehenden Composite-Filter: admin ODER picking ODER vorbau ODER fa_completion via ICurrentUserService) und auf `PrintBom` anwenden. +1 Filter-Test (Muster bestehender Filter-Tests).
- [ ] **Step 2: Vollsuite + Commit:**

```bash
git add -A
git commit -m "fix(auth): PrintBom auch fuer fa_completion (read-only BOM aus FA-Vervollstaendigung)"
```

---

### Task 5: Doku

**Files:** `IdealAkeWms/Views/Help/Changelog.cshtml`, `IdealAkeWms/Views/Help/Index.cshtml`, `docs/TESTSZENARIEN.md`, `CLAUDE.md`, `PROJECT_STATUS.md` (optional)

- [ ] **Step 1:** Changelog (v1.22.0-Card): Bullet "FA-Abarbeitungsliste filtert jetzt nach Arbeitsgang (statt Werkbank); die Werkbank wird als Spalte angezeigt. Jeder Benutzer kann im Profil seinen Standard-Arbeitsgang hinterlegen."
- [ ] **Step 2:** Hilfe Index: Abschnitt FA-Abarbeitungsliste anpassen (Arbeitsgang-Filter + Standard-Arbeitsgang im Profil).
- [ ] **Step 3:** TESTSZENARIEN Kapitel 38: Fall ergaenzen — Standard-Arbeitsgang im Profil setzen → Abarbeitungsliste oeffnet vorgefiltert; Liste zeigt FAs verschiedener Werkbaenke mit Werkbank-Spalte; Erledigt-Haken blendet aus.
- [ ] **Step 4:** CLAUDE.md: AppSettings/Filter unveraendert; Fallstrick/Hinweis ergaenzen: "FA-Abarbeitungsliste ist seit v1.22.0 Arbeitsgang-zentriert (nicht mehr Werkbank-zentriert); `User.DefaultWorkStepId` ist die Vorauswahl; das Werkbank-AG-Mapping (ProductionWorkplaceWorkSteps) ist davon nicht mehr betroffen."
- [ ] **Step 5:** Build + Vollsuite + Commit `docs: Arbeitsgang-Filter Abarbeitungsliste`.

---

### Task 6: Final-Check + Review

- [ ] **Step 1:** Build 0 Fehler; Tests gruen (Baseline + neue).
- [ ] **Step 2:** Sanity: `has-pending-model-changes` leer; SQL/70 + FreshInstall MigrationId + Spalte/FK; FaWorklist nutzt keinen Werkbank-Filter mehr; `/api/fa-work-steps/toggle-completed` unveraendert.
- [ ] **Step 3:** Final-Review-Subagent (read-only) ueber die Range.

### Task 7: PAUSE — User-Test (NICHT autonom mergen)
