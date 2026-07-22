# FA-Abschliessen-Fix (IsDonePicking wird nirgendwo gelesen) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Der "Abschliessen"-Button in FA-Liste + Leitstand wirkt wieder sichtbar: `IsDonePicking` wird ueberall gelesen, wo "erledigt" angezeigt oder gefiltert wird.

**Architektur:** Seit v1.11 (Commit `6b6e873`) schreibt `Picking/ToggleDone` das Flag `ProductionOrderPickingStatus.IsDonePicking` — aber Badge, Row-Graying, `showDone`-Filter und Picking-Worklist pruefen nur `ProductionOrders.IsDone` (Sage-Master). Fix: "erledigt" = `IsDone || IsDonePicking` an drei Stellen — (1) `GetForLeitstandAsync` (SQL-Filter + Projektion), (2) ViewModel-Mapping beider Listen-Controller, (3) `GetReleasedForPicking*`-Worklist-Queries. `IsDone` (Sage) wird NICHT zurueckgeschrieben — der Sage-Sync wuerde das ueberschreiben. Views brauchen KEINE Aenderung (binden `item.IsDone`, das jetzt kombiniert gemappt wird).

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10 (Navigation `ProductionOrder.PickingStatus`), xUnit + FluentAssertions + EF InMemory.

**Worktree:** `.claude/worktrees/missingparts-include-pd`, Branch `bugfix/missingparts-include-pd`. Baseline: HEAD `087e3c0`, 668 Web (1 skipped) + 99 Service gruen.

---

### Task 1: Repo — `GetForLeitstandAsync` filtert + liefert `IsDonePicking`

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs:5-16` (Record `LeitstandOrderRow`)
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs:40-41` (showDone-Filter) + `:59-75` (Projektion)
- Test: `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs` (falls nicht vorhanden: neu anlegen nach Muster bestehender Repo-Tests mit `TestDbContextFactory.Create()`)

- [ ] **Step 1: Failing Test schreiben**

```csharp
[Fact]
public async Task GetForLeitstand_ExcludesKommDoneOrders_WhenShowDoneFalse()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new ProductionOrderRepository(ctx);

    ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-OPEN", IsDone = false });
    ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-KOMMDONE", IsDone = false });
    ctx.ProductionOrders.Add(new ProductionOrder { Id = 3, OrderNumber = "FA-SAGEDONE", IsDone = true });
    ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 1, IsDonePicking = false });
    ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 2, IsDonePicking = true });
    await ctx.SaveChangesAsync();

    var page = await repo.GetForLeitstandAsync(null, null, null, showDone: false, page: 1, pageSize: 100);

    page.Rows.Should().ContainSingle(r => r.OrderNumber == "FA-OPEN");
    page.TotalCount.Should().Be(1);
}

[Fact]
public async Task GetForLeitstand_IncludesKommDoneWithFlag_WhenShowDoneTrue()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new ProductionOrderRepository(ctx);

    ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-OPEN", IsDone = false });
    ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-KOMMDONE", IsDone = false });
    ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 2, IsDonePicking = true });
    await ctx.SaveChangesAsync();

    var page = await repo.GetForLeitstandAsync(null, null, null, showDone: true, page: 1, pageSize: 100);

    page.Rows.Should().HaveCount(2);
    page.Rows.Single(r => r.OrderNumber == "FA-KOMMDONE").IsDonePicking.Should().BeTrue();
    page.Rows.Single(r => r.OrderNumber == "FA-OPEN").IsDonePicking.Should().BeFalse();
}
```

Hinweis: Pflichtfelder von `ProductionOrder`/`ProductionOrderPickingStatus` (z.B. Audit-Strings) gemaess Compilerfehlern/bestehenden Test-Seeds ergaenzen. Erst kompiliert der Test NICHT (Record hat kein `IsDonePicking`) — das ist der erwartete erste FAIL.

- [ ] **Step 2: Record erweitern** — `IProductionOrderRepository.cs`, Position NACH `bool IsDone,`:

```csharp
public record LeitstandOrderRow(
    int Id,
    string OrderNumber,
    decimal Quantity,
    string? Customer,
    string? ArticleNumber,
    string? Description1,
    string? Description2,
    DateTime? ProductionDate,
    DateTime? DeliveryDate,
    bool IsDone,
    bool IsDonePicking,
    string? WorkplaceName);
```

- [ ] **Step 3: Repo anpassen** — `ProductionOrderRepository.GetForLeitstandAsync`:

Filter (Zeile 40-41):
```csharp
if (!showDone)
    q = q.Where(o => !o.IsDone && (o.PickingStatus == null || !o.PickingStatus.IsDonePicking));
```

Projektion (Zeile 63-74) — neues Argument nach `o.IsDone`:
```csharp
.Select(o => new LeitstandOrderRow(
    o.Id,
    o.OrderNumber,
    o.Quantity,
    o.Customer,
    o.ArticleNumber,
    o.Description1,
    o.Description2,
    o.ProductionDate,
    o.DeliveryDate,
    o.IsDone,
    o.PickingStatus != null && o.PickingStatus.IsDonePicking,
    o.ProductionWorkplace != null ? o.ProductionWorkplace.Name : null))
```

- [ ] **Step 4: Compile-Fehler an allen `new LeitstandOrderRow(`-Aufrufstellen fixen** — `Grep "new LeitstandOrderRow"` ueber `IdealAkeWms.Tests/`: bestehende Test-Mocks bekommen `false` als neues 11. Argument (bzw. `true` wo ein Done-Szenario gemeint ist — Kontext lesen).

- [ ] **Step 5: Tests laufen lassen** — `dotnet test IdealAkeWms.slnx --filter "FullyQualifiedName~ProductionOrderRepositoryTests"` → beide neuen Tests PASS; danach Vollsuite gruen.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "fix(repo): GetForLeitstandAsync beruecksichtigt IsDonePicking (Filter + Projektion)"
```

---

### Task 2: Listen-Controller mappen kombiniertes `IsDone`

**Files:**
- Modify: `IdealAkeWms/Controllers/PickingLeitstandController.cs:109`
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs:116`
- Test: bestehende `PickingLeitstandControllerTests` / `ProductionOrdersControllerTests` (falls eine fehlt: nur fuer die vorhandene ergaenzen und das im Report vermerken — KEINEN neuen Test-Unterbau fuer Controller bauen, die Repo-Ebene ist durch Task 1 abgedeckt)

- [ ] **Step 1: Failing Test** — pro vorhandener Testklasse ein Test nach bestehendem Muster (Mock/InMemory ablesen!):

```csharp
[Fact]
public async Task Index_MapsIsDoneCombined_WhenIsDonePickingTrue()
{
    // Arrange nach Muster der Testklasse: Repo liefert eine LeitstandOrderRow
    // mit IsDone=false, IsDonePicking=true (showDone=true Pfad).
    // Act: Index aufrufen.
    // Assert: ViewModel-Item.IsDone == true
}
```

- [ ] **Step 2: FAIL verifizieren.**

- [ ] **Step 3: Mapping aendern** — beide Controller, im ViewModel-Mapping:

```csharp
IsDone = o.IsDone || o.IsDonePicking,
```

- [ ] **Step 4: PASS + Vollsuite gruen.**

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "fix(view-model): FA-Liste + Leitstand zeigen erledigt = IsDone || IsDonePicking"
```

---

### Task 3: Picking-Worklist schliesst Komm-erledigte FAs aus

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs:161-190`
- Test: bestehende Testklasse fuer dieses Repo (Glob `*PickingStatus*Tests*`; falls keine existiert: neu anlegen)

- [ ] **Step 1: Failing Tests**

```csharp
[Fact]
public async Task GetReleasedForPicking_ExcludesKommDoneOrders()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new ProductionOrderPickingStatusRepository(ctx);

    ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-1", IsDone = false });
    ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-2", IsDone = false });
    ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 1, IsReleasedForPicking = true, IsDonePicking = false });
    ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 2, IsReleasedForPicking = true, IsDonePicking = true });
    await ctx.SaveChangesAsync();

    var list = await repo.GetReleasedForPickingAsync();
    var count = await repo.GetReleasedForPickingCountAsync();

    list.Should().ContainSingle(p => p.OrderNumber == "FA-1");
    count.Should().Be(1);
}

[Fact]
public async Task GetReleasedForPickingByPicker_ExcludesKommDoneOrders()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new ProductionOrderPickingStatusRepository(ctx);

    ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-1", IsDone = false });
    ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-2", IsDone = false });
    ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 1, IsReleasedForPicking = true, AssignedPickerId = 7, IsDonePicking = false });
    ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 2, IsReleasedForPicking = true, AssignedPickerId = 7, IsDonePicking = true });
    await ctx.SaveChangesAsync();

    var list = await repo.GetReleasedForPickingByPickerAsync(7);

    list.Should().ContainSingle(p => p.OrderNumber == "FA-1");
}
```

- [ ] **Step 2: FAIL verifizieren.**

- [ ] **Step 3: Queries anpassen**

`GetReleasedForPickingAsync` (Zeile 164):
```csharp
.Where(p => p.PickingStatus != null && p.PickingStatus.IsReleasedForPicking
            && !p.IsDone && !p.PickingStatus.IsDonePicking)
```

`GetReleasedForPickingByPickerAsync` (Zeile 176-179):
```csharp
.Where(p => p.PickingStatus != null
            && p.PickingStatus.IsReleasedForPicking
            && !p.IsDone
            && !p.PickingStatus.IsDonePicking
            && p.PickingStatus.AssignedPickerId == pickerId)
```

`GetReleasedForPickingCountAsync` (Zeile 188-190):
```csharp
=> _context.ProductionOrderPickingStatuses
    .CountAsync(s => s.IsReleasedForPicking && !s.ProductionOrder.IsDone && !s.IsDonePicking);
```

`GetMaxPickingPriorityAsync` bleibt UNVERAENDERT (Prioritaets-Luecken durch erledigte FAs sind harmlos; Minimal-Eingriff).

- [ ] **Step 4: PASS + Vollsuite gruen.**

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "fix(repo): Picking-Worklist schliesst IsDonePicking-FAs aus"
```

---

### Task 4: Version v1.21.1 + Doku

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs` → `1.21.1`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` → neue v1.21.1-Card ganz oben (Bugfix: "FA ueber Abschliessen-Button abschliessen wirkt wieder — erledigt-Anzeige und Filter beruecksichtigen jetzt den App-Komm-Status (IsDonePicking), nicht nur den Sage-Status")
- Modify: `CLAUDE.md` → neuer Fallstrick-Bullet: "**IsDone vs IsDonePicking — Lese-Seite (seit v1.21.1)**: `Picking/ToggleDone` schreibt `PickingStatus.IsDonePicking` (Sage-`IsDone` darf nicht beschrieben werden — Sync wuerde es ueberschreiben). ALLE Stellen, die 'FA erledigt' anzeigen oder filtern (FA-Liste, Leitstand, Picking-Worklist), muessen `IsDone || IsDonePicking` pruefen. Von v1.11 bis v1.21.0 las keine Query das Flag — der Abschliessen-Button war wirkungslos."
- Modify: `docs/TESTSZENARIEN.md` → Szenario in Kapitel 36 ergaenzen (oder neues Kapitel 37): FA im Leitstand abschliessen → Zeile verschwindet (Default-Filter) bzw. wird grau+Erledigt (showDone) → FA verschwindet aus Kommissionierungs-Worklist → erneutes Oeffnen ueber "Erledigte anzeigen" + Klick macht FA wieder offen
- Modify: `PROJECT_STATUS.md` → v1.21.1-Zeile

- [ ] **Step 1: Alle Doku-Dateien anpassen** (bestehende Formate exakt uebernehmen)
- [ ] **Step 2: Build + Vollsuite gruen**
- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "docs: Version v1.21.1 + Changelog + Fallstrick IsDone vs IsDonePicking"
```

---

### Task 5: Final-Check

- [ ] `dotnet build IdealAkeWms.slnx` → 0 Fehler
- [ ] `dotnet test IdealAkeWms.slnx --no-build` → mind. 668+5 Web gruen, 99 Service gruen
- [ ] `git log --oneline 087e3c0..HEAD` → 4 Commits, Worktree clean
- [ ] PAUSE: User testet manuell (Leitstand + FA-Liste + Picker-Worklist). KEIN Merge ohne User-Bestaetigung.
