# FaWorkStep Completion-Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Den einen `FaWorkStep.IsCompleted`-Status in zwei trennen: `IsSpecComplete` ("vollstaendig definiert", gesetzt in FA-Vervollstaendigung, blendet NICHT aus) und `IsCompleted` ("Arbeit erledigt", gesetzt in FA-Abarbeitungsliste, blendet aus).

**Architecture:** Neues Bool-Feld `IsSpecComplete` (+ Audit `SpecCompletedAt/By`) auf `FaWorkStep`. FA-Vervollstaendigung schreibt kuenftig `IsSpecComplete` statt `IsCompleted`; ihre Uebersicht zaehlt Spec-Fortschritt. Die FA-Abarbeitungsliste bleibt unveraendert (nutzt weiter `IsCompleted` fuer Checkbox + Ausblende-Logik). Migration verschiebt bestehende `IsCompleted`-Werte nach `IsSpecComplete` (das alte v1.13-/Konvertierungs-`IsCompleted` war semantisch "Spec fertig") und setzt `IsCompleted` zurueck.

**Tech Stack:** ASP.NET Core 10 MVC + EF Core 10 (SQL Server), xUnit + FluentAssertions + Moq + EF InMemory.

**Rahmen:** Worktree `.claude/worktrees/missingparts-include-pd`, Branch `bugfix/missingparts-include-pd`, HEAD `92a5cbc`. Faltet in v1.22.0 (noch nicht released) — KEIN Versions-Bump, nur Changelog-Bullet. Migration Nummer **69**. Baseline: 686 Web (1 skipped) + 104 Service gruen.

**Was UNVERAENDERT bleibt (bewusst):** `FaWorklistController` (Checkbox `/api/fa-work-steps/toggle-completed` → `SetIsCompletedAsync`, Ausblende-Logik `mappedSteps.All(f => f.IsCompleted)`), `FaWorkStepsApiController`, Leitstand. Die Abarbeitungsliste filtert weiter NUR nach Arbeit-erledigt.

---

### Task 0: Pre-Flight

- [ ] **Step 1:** `git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd log --oneline -1` → erwartet `92a5cbc` (oder neuer), Worktree clean.
- [ ] **Step 2:** Baseline: `dotnet build IdealAkeWms.slnx` (0 Fehler) + `dotnet test IdealAkeWms.slnx --no-build` → erwartet 686 Web (1 skipped) + 104 Service gruen. Zahlen notieren.

---

### Task 1: Model + Repository + Migration (TDD)

**Files:**
- Modify: `IdealAkeWms/Models/FaWorkStep.cs`
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs` (FaWorkStep-Block: SpecCompletedBy MaxLength)
- Modify: `IdealAkeWms/Data/Repositories/IFaWorkStepRepository.cs` (record `FaWorkStepCounts` + neue Methode)
- Modify: `IdealAkeWms/Data/Repositories/FaWorkStepRepository.cs`
- Migration: `dotnet ef migrations add SplitFaWorkStepCompletion`
- Test: `IdealAkeWms.Tests/Repositories/FaWorkStepRepositoryTests.cs`

- [ ] **Step 1: Model erweitern** — `FaWorkStep.cs`, nach den `CompletedBy`-Feldern (Zeile 19):

```csharp
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }

    /// <summary>Spec/Definition fertig (FA-Vervollstaendigung). Blendet NICHT aus.</summary>
    public bool IsSpecComplete { get; set; }
    public DateTime? SpecCompletedAt { get; set; }
    public string? SpecCompletedBy { get; set; }
```

- [ ] **Step 2: DbContext** — im bestehenden `modelBuilder.Entity<FaWorkStep>(...)`-Block die MaxLength fuer den neuen Audit-String ergaenzen (analog `CompletedBy`, falls dort `HasMaxLength(200)` gesetzt ist — sonst weglassen; pruefen wie `CompletedBy` konfiguriert ist und identisch behandeln):

```csharp
    e.Property(f => f.SpecCompletedBy).HasMaxLength(200);
```

- [ ] **Step 3: Repository-Counts umstellen** — `IFaWorkStepRepository.cs` Zeile 6, das record-Feld `CompletedCount` semantisch auf Spec-Fertig umstellen (Name beibehalten waere irrefuehrend → umbenennen):

```csharp
public record FaWorkStepCounts(int ActiveCount, int SpecCompleteCount, int SpecCount);
```

Und neue Methode im Interface (nach `SetIsCompletedAsync`, Zeile 23):

```csharp
    /// <summary>Setzt IsSpecComplete + SpecCompletedAt/By bzw. null (FA-Vervollstaendigung).</summary>
    Task SetIsSpecCompleteAsync(int faWorkStepId, bool value, string modifiedBy, string modifiedByWindows);
```

- [ ] **Step 4: Failing Tests** in `FaWorkStepRepositoryTests.cs`:

```csharp
[Fact]
public async Task SetIsSpecComplete_SetsSpecFieldsNotWorkDone()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new FaWorkStepRepository(ctx);
    ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
    ctx.WorkSteps.Add(new WorkStep { Id = 10, Code = "VE", Name = "Elektro" });
    var row = new FaWorkStep { ProductionOrderId = 1, WorkStepId = 10 };
    ctx.FaWorkSteps.Add(row);
    await ctx.SaveChangesAsync();

    await repo.SetIsSpecCompleteAsync(row.Id, true, "tester", "win\\tester");

    var reloaded = await ctx.FaWorkSteps.FindAsync(row.Id);
    reloaded!.IsSpecComplete.Should().BeTrue();
    reloaded.SpecCompletedAt.Should().NotBeNull();
    reloaded.SpecCompletedBy.Should().Be("tester");
    reloaded.IsCompleted.Should().BeFalse(); // Arbeit-erledigt unberuehrt
}

[Fact]
public async Task GetCounts_SpecCompleteCount_CountsSpecCompleteNotWorkDone()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new FaWorkStepRepository(ctx);
    ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
    ctx.WorkSteps.AddRange(new WorkStep { Id = 10, Code = "VE", Name = "E" }, new WorkStep { Id = 11, Code = "VL", Name = "L" });
    ctx.FaWorkSteps.AddRange(
        new FaWorkStep { ProductionOrderId = 1, WorkStepId = 10, IsSpecComplete = true, IsCompleted = false },
        new FaWorkStep { ProductionOrderId = 1, WorkStepId = 11, IsSpecComplete = false, IsCompleted = true });
    await ctx.SaveChangesAsync();

    var counts = await repo.GetCountsByProductionOrderIdsAsync(new List<int> { 1 });

    counts[1].ActiveCount.Should().Be(2);
    counts[1].SpecCompleteCount.Should().Be(1); // nur die spec-fertige Zeile
}
```

- [ ] **Step 5: FAIL verifizieren** — `dotnet test IdealAkeWms.slnx --filter "FullyQualifiedName~FaWorkStepRepositoryTests"` (kompiliert erst nach Step 6/7 → erster FAIL ist Compile).

- [ ] **Step 6: Repo implementieren** — `FaWorkStepRepository.cs`:
  - In `GetCountsByProductionOrderIdsAsync` (Zeile 70-80): Projektion + Aggregat auf `IsSpecComplete` umstellen:

```csharp
            var rows = await _context.FaWorkSteps
                .Where(f => chunk.Contains(f.ProductionOrderId) && !f.IsRemoved)
                .Select(f => new { f.ProductionOrderId, f.IsSpecComplete, SpecCount = f.Specs.Count })
                .ToListAsync();

            foreach (var grp in rows.GroupBy(r => r.ProductionOrderId))
            {
                result[grp.Key] = new FaWorkStepCounts(
                    ActiveCount: grp.Count(),
                    SpecCompleteCount: grp.Count(r => r.IsSpecComplete),
                    SpecCount: grp.Sum(r => r.SpecCount));
            }
```

  - Neue Methode (nach `SetIsCompletedAsync`, ~Zeile 130, Muster identisch aber Spec-Felder):

```csharp
    public async Task SetIsSpecCompleteAsync(int faWorkStepId, bool value, string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.FaWorkSteps.FirstOrDefaultAsync(f => f.Id == faWorkStepId)
            ?? throw new InvalidOperationException($"FaWorkStep row missing for Id {faWorkStepId}.");

        row.IsSpecComplete = value;
        row.SpecCompletedAt = value ? DateTime.Now : null;
        row.SpecCompletedBy = value ? modifiedBy : null;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }
```

- [ ] **Step 7: Aufrufer von `CompletedCount` anpassen** — `Grep "CompletedCount"` ueber `IdealAkeWms/`: der einzige Konsument ist `FaCompletionController.Index` (Mapping der Uebersichts-Spalte). Dort `c?.CompletedCount` → `c?.SpecCompleteCount` aendern (die Property-Verwendung; ViewModel-Feld kann `CompletedCount` heissen — nur die Repo-Quelle wechselt). Falls der Build weitere Stellen meldet: alle auf `SpecCompleteCount` umbiegen.

- [ ] **Step 8: PASS** — Repo-Tests gruen, dann Build der App gruen (Controller-Anpassung aus Step 7 noetig fuer Compile).

- [ ] **Step 9: Migration generieren** — `dotnet ef migrations add SplitFaWorkStepCompletion --project IdealAkeWms`. In `Up()` NACH den `AddColumn`-Aufrufen die Daten-Verschiebung einfuegen:

```csharp
            // Altes IsCompleted war semantisch "Spec fertig" (v1.13-/Konvertierungs-Herkunft).
            // -> nach IsSpecComplete uebernehmen, Arbeit-erledigt frisch starten.
            migrationBuilder.Sql(@"
UPDATE dbo.FaWorkSteps
SET IsSpecComplete = IsCompleted,
    SpecCompletedAt = CompletedAt,
    SpecCompletedBy = CompletedBy,
    IsCompleted = 0,
    CompletedAt = NULL,
    CompletedBy = NULL;");
```

`Down()` von EF generiert lassen (Spalten-Drop genuegt).

- [ ] **Step 10: Vollsuite + Commit:**

```bash
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
dotnet test IdealAkeWms.slnx --no-build 2>&1 | tail -4
git add -A
git commit -m "feat(model): FaWorkStep.IsSpecComplete getrennt von IsCompleted + Migration 69"
```

---

### Task 2: SQL/69 + FreshInstall

**Files:**
- Create: `SQL/69_SplitFaWorkStepCompletion.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1:** `SQL/69_SplitFaWorkStepCompletion.sql` — idempotent (Muster der bestehenden `SQL/6x`-Skripte): 3 Spalten via `IF COL_LENGTH('dbo.FaWorkSteps','IsSpecComplete') IS NULL ALTER TABLE ... ADD ...` (BIT NOT NULL DEFAULT 0, DATETIME2 NULL, NVARCHAR(200) NULL), in separatem Batch das `UPDATE`-Statement aus Task 1 Step 9, dann `__EFMigrationsHistory`-INSERT mit der ECHTEN generierten MigrationId (`<timestamp>_SplitFaWorkStepCompletion`) in eigenem Batch mit `IF NOT EXISTS`-Guard.

- [ ] **Step 2:** `SQL/00_FreshInstall.sql` — im `FaWorkSteps`-CREATE-TABLE-Block die 3 neuen Spalten ergaenzen (`[IsSpecComplete] BIT NOT NULL CONSTRAINT DF_FaWorkSteps_IsSpecComplete DEFAULT 0`, `[SpecCompletedAt] DATETIME2 NULL`, `[SpecCompletedBy] NVARCHAR(200) NULL`) UND die neue MigrationId im `__EFMigrationsHistory`-INSERT-Block ergaenzen. **Beide Stellen** (bekannter Fallstrick). Das UPDATE-Statement gehoert NICHT in FreshInstall (frische DB hat keine Alt-Daten).

- [ ] **Step 3: Commit:**

```bash
git add SQL/69_SplitFaWorkStepCompletion.sql SQL/00_FreshInstall.sql
git commit -m "feat(sql): SQL/69 + FreshInstall fuer IsSpecComplete-Split"
```

---

### Task 3: FA-Vervollstaendigung auf IsSpecComplete (TDD)

**Files:**
- Modify: `IdealAkeWms/Controllers/FaCompletionController.cs` (ToggleIsCompleted-Action + Tab-Mapping)
- Modify: `IdealAkeWms/Models/ViewModels/FaCompletionEditViewModel.cs` (Tab-Felder)
- Modify: `IdealAkeWms/Views/FaCompletion/Edit.cshtml` (Switch + Tab-Indikatoren)
- Test: `IdealAkeWms.Tests/Controllers/FaCompletionControllerTests.cs`

- [ ] **Step 1: ViewModel** — `FaCompletionEditViewModel.cs`, im Tab-Typ (der die Kachel beschreibt) die drei Felder umbenennen: `IsCompleted` → `IsSpecComplete`, `CompletedAt` → `SpecCompletedAt`, `CompletedBy` → `SpecCompletedBy`. (Falls der Tab-Typ `AssemblyGroupTabViewModel`/`FaWorkStepTabViewModel` o.ae. heisst — bestehenden Namen beibehalten, nur die 3 Properties umbenennen.)

- [ ] **Step 2: Failing Test** in `FaCompletionControllerTests.cs` — bestehenden `ToggleIsCompleted`-Test (falls vorhanden) auf neue Semantik umstellen + sicherstellen, dass `SetIsSpecCompleteAsync` (nicht `SetIsCompletedAsync`) aufgerufen wird:

```csharp
[Fact]
public async Task ToggleSpecComplete_SetsSpecCompleteViaRepo()
{
    // Arrange nach Muster der Bestandsklasse: Mock IFaWorkStepRepository,
    // GetByIdAsync liefert FaWorkStep { Id=5, ProductionOrderId=1, IsSpecComplete=false,
    //   WorkStep = new WorkStep { Code="VE" } }.
    // Act: ctrl.ToggleSpecComplete(5)
    // Assert: RedirectToAction Edit; repo.Verify(r => r.SetIsSpecCompleteAsync(5, true,
    //   It.IsAny<string>(), It.IsAny<string>()), Times.Once);
}
```

- [ ] **Step 3: FAIL verifizieren.**

- [ ] **Step 4: Controller** — `FaCompletionController.cs`:
  - Action `ToggleIsCompleted` (Zeile 436-460) umbenennen zu `ToggleSpecComplete`, `newValue = !row.IsSpecComplete`, Aufruf `SetIsSpecCompleteAsync`, TempData-Texte: `"Arbeitsgang als vollstaendig definiert markiert."` bzw. `"Definition zurueckgesetzt."`
  - Tab-Mapping (Zeile 189-194): `IsCompleted = f.IsCompleted` → `IsSpecComplete = f.IsSpecComplete`, `CompletedAt = f.CompletedAt` → `SpecCompletedAt = f.SpecCompletedAt`, `CompletedBy = f.CompletedBy` → `SpecCompletedBy = f.SpecCompletedBy`.
  - XML-Doc-Kommentar (Zeile 16) sinngemaess anpassen.

- [ ] **Step 5: View** — `FaCompletion/Edit.cshtml`:
  - Tab-Indikator (Zeile 70-71): `t.IsCompleted` → `t.IsSpecComplete` (✓/●, success/warning).
  - Status-Switch (Zeile 115-130): `asp-action="ToggleIsCompleted"` → `asp-action="ToggleSpecComplete"`; `id="chkCompleted_..."`/`@active.IsCompleted` → `IsSpecComplete`; Label `<strong>Vervollständigt</strong> (<code>IsCompleted</code>)` → `<strong>Vollständig definiert</strong>`; den `<code>`-Hinweis entfernen oder auf `IsSpecComplete` setzen; CompletedBy/CompletedAt-Anzeige → `SpecCompletedBy`/`SpecCompletedAt`. Optional Hilfetext: `<span class="text-muted small d-block">Markiert die Merkmal-/AG-Definition als fertig — blendet die FA NICHT aus der Abarbeitungsliste aus.</span>`

- [ ] **Step 6: PASS + Vollsuite.**

- [ ] **Step 7: Commit:**

```bash
git add -A
git commit -m "feat(fa-completion): Vervollstaendigung schreibt IsSpecComplete (nicht Arbeit-erledigt)"
```

---

### Task 4: Doku

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `CLAUDE.md`

- [ ] **Step 1: Changelog** — in der bestehenden v1.22.0-Card einen Bullet ergaenzen: "FA-Vervollstaendigen und FA-Abarbeitungsliste haben jetzt getrennte Status: 'Vollstaendig definiert' (Merkmale/AGs festgelegt — blendet nicht aus) in der Vervollstaendigung, 'Erledigt' (Vorbau gebaut — blendet aus) in der Abarbeitungsliste."

- [ ] **Step 2: Hilfe** — `Views/Help/Index.cshtml` im FA-Vervollstaendigung-/FA-Abarbeitungslisten-Abschnitt klarstellen: "Vollstaendig definiert" = Spec/Merkmale fertig; "Erledigt" in der Abarbeitungsliste = Vorbau physisch gebaut, blendet die FA bei dieser Werkbank aus (ueber "Erledigte anzeigen" wieder sichtbar).

- [ ] **Step 3: TESTSZENARIEN** — Kapitel 38 um einen Fall ergaenzen: FA in Vervollstaendigung auf "Vollstaendig definiert" setzen → FA bleibt in der Abarbeitungsliste sichtbar; erst der "Erledigt"-Haken in der Abarbeitungsliste blendet sie aus.

- [ ] **Step 4: CLAUDE.md** — den FaWorkSteps-Fallstrick (v1.22.0) ergaenzen: "Zwei Completion-Status auf FaWorkStep: `IsSpecComplete` (FA-Vervollstaendigung, 'Vollstaendig definiert', blendet NICHT aus) vs `IsCompleted` (FA-Abarbeitungsliste, 'Erledigt', Ausblende-Logik `mappedSteps.All(IsCompleted)`). Migration 69 verschob das alte IsCompleted nach IsSpecComplete."

- [ ] **Step 5: Build + Vollsuite + Commit:**

```bash
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
dotnet test IdealAkeWms.slnx --no-build 2>&1 | tail -4
git add -A
git commit -m "docs: Completion-Split (IsSpecComplete vs IsCompleted) in Changelog/Hilfe/CLAUDE.md/TESTSZENARIEN"
```

---

### Task 5: Final-Check + Review

- [ ] **Step 1:** `dotnet build IdealAkeWms.slnx` (0 Fehler) + `dotnet test IdealAkeWms.slnx --no-build` (mind. 688 Web + 104 Service gruen — Baseline 686 + 2 Repo + ggf. 1 Controller).
- [ ] **Step 2:** Sanity-Greps: kein verbliebenes `t.IsCompleted`/`active.IsCompleted` in `FaCompletion/Edit.cshtml`; `FaWorklistController` UNVERAENDERT (nutzt weiter `IsCompleted` + `toggle-completed`); SQL/69 MigrationId == generierter Migrationsname; FreshInstall enthaelt die 3 Spalten + History-Eintrag.
- [ ] **Step 3:** Final-Review-Subagent (read-only): Trennung sauber (Vervollstaendigung→IsSpecComplete, Worklist→IsCompleted)? Migration-Datenmove korrekt? Keine ungewollte Verhaltensaenderung in der Abarbeitungsliste?

### Task 6: PAUSE — User-Test

- [ ] User testet: FA in Vervollstaendigung "Vollstaendig definiert" setzen → bleibt in Abarbeitungsliste; "Erledigt" dort → verschwindet. Merge/Worktree NUR nach expliziter Freigabe.
