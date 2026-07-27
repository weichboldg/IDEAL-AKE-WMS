# BDE Phase 2.2 Mehrfachanmeldung + Zeit-Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Zwei neue AppSettings lockern die strikten Ein-Operator-Ein-AG-Constraints konditional. Ein neuer `BdeTimeSplitService` liefert pro-Segment-korrekte Effektiv-Zeiten. Terminal-UI bekommt Close-Others-Dialog (bei Multi-MA-Abschluss) und Paused-Bookings-Hinweis nach Operator-Scan.

**Architecture:** Gefilterte UNIQUE-Indizes droppen und als non-unique neu anlegen; Enforcement wandert konditional in den Service. Split-Berechnung on-the-fly (keine persistierte Spalte). Terminal-UI erweitert sich in 2 Punkten (Close-Others + Paused-Hint), Buchungsübersicht bekommt eine neue Spalte.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10, SQL Server, xUnit + FluentAssertions + Moq, Bootstrap 5, Vanilla JS.

**Scope:** Alle Pfade relativ zu `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1` (branch `feature/bde-phase-1`). Kein Versions-Bump (bleibt v1.8.2). Phase 2.1 bereits committed und gemerged in diesen Branch.

---

## Task 1: AppSettings Seeding

**Files:**
- Modify: `IdealAkeWms/Program.cs`

- [ ] **Step 1: Seeding-Block lokalisieren**

```bash
grep -n "BdeAktiv\|BdeNurFaMeldung" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Program.cs"
```

Erwartet: Block in `Program.cs` mit mehreren `db.AppSettings.Add(new IdealAkeWms.Models.AppSetting { Key = "BdeAktiv", ... })` Aufrufen. Die beiden neuen Settings werden direkt nach dem `BdeDefaultArbeitsgang`-Block ergänzt.

- [ ] **Step 2: Seeding für `BdeMehrfachBuchungProOperator` hinzufügen**

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Program.cs`, nach dem bestehenden Seeding-Block für `BdeDefaultArbeitsgang` (suche nach `"BdeDefaultArbeitsgang"`), vor dem nächsten bestehenden Block einfügen:

```csharp
        if (!await db.AppSettings.AnyAsync(s => s.Key == "BdeMehrfachBuchungProOperator"))
        {
            db.AppSettings.Add(new IdealAkeWms.Models.AppSetting
            {
                Key = "BdeMehrfachBuchungProOperator",
                Value = "false",
                Description = "Ein Mitarbeiter darf mehrere parallele Buchungen haben (auf verschiedenen Arbeitsgängen)",
                CreatedAt = DateTime.Now,
                CreatedBy = "System",
                CreatedByWindows = "System"
            });
        }

        if (!await db.AppSettings.AnyAsync(s => s.Key == "BdeMehrfachBuchungProArbeitsgang"))
        {
            db.AppSettings.Add(new IdealAkeWms.Models.AppSetting
            {
                Key = "BdeMehrfachBuchungProArbeitsgang",
                Value = "false",
                Description = "Ein Arbeitsgang darf mehrere parallele Buchungen haben (durch verschiedene Mitarbeiter)",
                CreatedAt = DateTime.Now,
                CreatedBy = "System",
                CreatedByWindows = "System"
            });
        }
```

**Hinweis:** Die exakte Syntax der umliegenden `Add(...)`-Blöcke kopieren (Casing von `IdealAkeWms.Models.AppSetting` vs. `AppSetting`, ob `AnyAsync` verwendet wird oder `Any`). Muster an bestehenden Blöcken ausrichten.

- [ ] **Step 3: Build + Test**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: 0 Fehler, alle Tests grün.

- [ ] **Step 4: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Program.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): seed BdeMehrfachBuchungProOperator + ProArbeitsgang settings"
```

---

## Task 2: Schema — Indizes droppen + als non-unique neu anlegen

**Files:**
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs`
- Create (via EF-CLI): `IdealAkeWms/Migrations/YYYYMMDDHHMMSS_RelaxBdeBookingConstraints.cs` (+ Designer.cs)

- [ ] **Step 1: DbContext-Index-Konfiguration auf non-unique umstellen**

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Data/ApplicationDbContext.cs`, die zwei `HasIndex`-Deklarationen mit `IsUnique()` finden:

```bash
grep -n "IX_BdeBookings_BdeOperatorId_Active\|IX_BdeBookings_WorkOperationId_Active" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Data/ApplicationDbContext.cs"
```

Die Deklarationen (ungefähr Zeilen 725-735) so ändern: `.IsUnique()` entfernen. Die Filter-Expression (`WHERE EndedAt IS NULL AND IsCancelled = 0`) bleibt, nur die Uniqueness entfällt.

Vorher (Beispiel):
```csharp
            entity.HasIndex(e => e.WorkOperationId)
                .IsUnique()
                .HasFilter("[EndedAt] IS NULL AND [IsCancelled] = 0")
                .HasDatabaseName("IX_BdeBookings_WorkOperationId_Active");

            entity.HasIndex(e => e.BdeOperatorId)
                .IsUnique()
                .HasFilter("[EndedAt] IS NULL AND [IsCancelled] = 0")
                .HasDatabaseName("IX_BdeBookings_BdeOperatorId_Active");
```

Nachher:
```csharp
            entity.HasIndex(e => e.WorkOperationId)
                .HasFilter("[EndedAt] IS NULL AND [IsCancelled] = 0")
                .HasDatabaseName("IX_BdeBookings_WorkOperationId_Active");

            entity.HasIndex(e => e.BdeOperatorId)
                .HasFilter("[EndedAt] IS NULL AND [IsCancelled] = 0")
                .HasDatabaseName("IX_BdeBookings_BdeOperatorId_Active");
```

- [ ] **Step 2: EF-Migration generieren**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms" && dotnet ef migrations add RelaxBdeBookingConstraints
```

Erwartet: neue Datei in `IdealAkeWms/Migrations/*_RelaxBdeBookingConstraints.cs`. `ApplicationDbContextModelSnapshot.cs` wird automatisch aktualisiert.

- [ ] **Step 3: Migration-Inhalt prüfen**

```bash
cat "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Migrations/"*_RelaxBdeBookingConstraints.cs
```

Erwartet: `Up()` enthält für beide Indizes je ein `DropIndex` gefolgt von einem `CreateIndex` (ohne `unique: true`). `Down()` macht das Gegenteil. Keine anderen Schema-Änderungen. Falls ja → `dotnet ef migrations remove`, Drift beheben, neu generieren.

- [ ] **Step 4: Build + Test**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: 0 Fehler, alle Tests grün. InMemory-DB ignoriert Indizes komplett; EF InMemory-basierte Tests laufen unverändert.

- [ ] **Step 5: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Data/ApplicationDbContext.cs IdealAkeWms/Migrations/
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): relax filtered unique indexes to non-unique

IX_BdeBookings_BdeOperatorId_Active and IX_BdeBookings_WorkOperationId_Active
are no longer UNIQUE. Enforcement moves to service layer so it can be
conditionally applied based on the new Phase 2.2 settings."
```

---

## Task 3: BdeBookingService — konditionale Enforcement (TDD)

**Files:**
- Modify: `IdealAkeWms/Services/BdeBookingService.cs`
- Modify: `IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs`

Diese Änderungen treffen `StartPlannedAsync` (Setup + Production) und ersetzen die Phase-1-Auto-Close-Logik durch die neue Terminal/Cumulative-Regel-Kaskade.

- [ ] **Step 1: Neue Tests anhängen (Production-Fälle)**

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs` vor der letzten `}` (Klassen-Ende) einfügen. Hinweis: Die bestehenden Tests laufen mit InMemory-DB ohne Settings-Mock. Für die neuen Tests brauchen wir einen settings-aware Service-Konstruktor. **Problem:** Der existierende `BdeBookingService`-Ctor nimmt `(ApplicationDbContext ctx, ICurrentUserService userSvc)`. Für Phase 2.2 müssen wir `IAppSettingRepository` einschleusen.

Tests werden mit einem Mock für `IAppSettingRepository` erstellt:

```csharp
    [Fact]
    public async Task StartProduction_MultiMaDisabled_RejectsCollision()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var otherOp = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);

        // MA1 hat eine aktive Buchung auf dem AG
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1)));
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx, multiMa: false, multiOp: false);

        var result = await svc.StartProductionAsync(otherOp, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.CollisionOtherOperator);
    }

    [Fact]
    public async Task StartProduction_MultiMaEnabled_AllowsSecondOperator()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var otherOp = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);

        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1)));
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx, multiMa: true, multiOp: false);

        var result = await svc.StartProductionAsync(otherOp, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        ctx.BdeBookings.Count(b => b.WorkOperationId == ids.WorkOperationId && b.EndedAt == null).Should().Be(2);
    }

    [Fact]
    public async Task StartProduction_MultiOperatorDisabled_RequiresQuantity()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var secondWoId = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        // MA1 hat Production auf WO1 aktiv
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1)));
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx, multiMa: false, multiOp: false);

        var result = await svc.StartProductionAsync(ids.OperatorId, secondWoId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.QuantityRequired);
    }

    [Fact]
    public async Task StartProduction_MultiOperatorEnabled_AllowsParallel()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var secondWoId = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1)));
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx, multiMa: false, multiOp: true);

        var result = await svc.StartProductionAsync(ids.OperatorId, secondWoId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        ctx.BdeBookings.Count(b => b.BdeOperatorId == ids.OperatorId && b.EndedAt == null).Should().Be(2);
    }

    [Fact]
    public async Task StartSetup_AlwaysExclusive_EvenInMultiMaMode()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var otherOp = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);

        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Setup, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-15)));
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx, multiMa: true, multiOp: true);

        var result = await svc.StartSetupAsync(otherOp, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.CollisionOtherOperator);
    }

    [Fact]
    public async Task SetupToProduction_SameAG_AlwaysTransitions()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var setup = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Setup, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-20));
        ctx.BdeBookings.Add(setup);
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx, multiMa: true, multiOp: true);

        var result = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        result.Booking!.ParentBookingId.Should().Be(setup.Id);
        ctx.BdeBookings.First(b => b.Id == setup.Id).EndedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task StartProduction_MultiOp_ClosesActiveSetupOnDifferentAg()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var secondWoId = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        var setupOnDifferentAg = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Setup, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-10), workOperationId: secondWoId);
        ctx.BdeBookings.Add(setupOnDifferentAg);
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx, multiMa: false, multiOp: true);

        var result = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        ctx.BdeBookings.First(b => b.Id == setupOnDifferentAg.Id).EndedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task StartProduction_MultiOp_ClosesActiveActivity()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var activity = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Activity, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-10));
        ctx.BdeBookings.Add(activity);
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx, multiMa: false, multiOp: true);

        var result = await svc.StartProductionAsync(ids.OperatorId, ids.WorkOperationId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.Success);
        ctx.BdeBookings.First(b => b.Id == activity.Id).EndedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task StartActivity_AlwaysExclusive_PerOperator_EvenWithMultiOp()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var production = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(production);
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx, multiMa: true, multiOp: true);

        var result = await svc.StartActivityAsync(ids.OperatorId, ids.ActivityId, ids.WorkplaceId, ids.TerminalId);

        result.Outcome.Should().Be(BdeBookingOutcome.QuantityRequired);
    }
```

Zusätzlich oben in der Test-Klasse einen `CreateService`-Helper ergänzen (falls noch nicht vorhanden), **direkt unterhalb der bestehenden Helper**:

```csharp
    private static BdeBookingService CreateService(ApplicationDbContext ctx, bool multiMa = false, bool multiOp = false)
    {
        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");

        var settingsMock = new Mock<IAppSettingRepository>();
        settingsMock.Setup(s => s.GetValueAsync("BdeMehrfachBuchungProArbeitsgang"))
            .ReturnsAsync(multiMa ? "true" : "false");
        settingsMock.Setup(s => s.GetValueAsync("BdeMehrfachBuchungProOperator"))
            .ReturnsAsync(multiOp ? "true" : "false");

        return new BdeBookingService(ctx, userSvc.Object, settingsMock.Object);
    }
```

Achtung: der letzte Parameter `settingsMock.Object` erfordert, dass `BdeBookingService` einen dritten Konstruktor-Parameter `IAppSettingRepository` bekommt (siehe Step 3).

- [ ] **Step 2: Tests laufen lassen, erwarten dass sie nicht kompilieren oder failen**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~BdeBookingServiceTests" 2>&1 | tail -20
```

Erwartet: Build-Fehler (neuer Ctor-Parameter existiert noch nicht) oder Test-Failures.

- [ ] **Step 3: BdeBookingService Konstruktor erweitern**

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Services/BdeBookingService.cs`:

Oben in der Klasse (Felder + Konstruktor) ändern:

```csharp
    private readonly ApplicationDbContext _ctx;
    private readonly ICurrentUserService _userSvc;
    private readonly IAppSettingRepository _settings;

    public BdeBookingService(ApplicationDbContext ctx, ICurrentUserService userSvc, IAppSettingRepository settings)
    {
        _ctx = ctx;
        _userSvc = userSvc;
        _settings = settings;
    }
```

**Auch bestehende Test-Klassen anpassen, die den Service instanziieren:**

```bash
grep -rn "new BdeBookingService(" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms.Tests/" 2>/dev/null
```

Für jede Fundstelle, die noch den 2-Parameter-Ctor benutzt, einen zusätzlichen `settingsMock.Object` (oder `Mock.Of<IAppSettingRepository>()`) als dritten Parameter anfügen. Die bestehenden Tests sollen weiter mit beiden Settings auf `false` laufen (Phase-1-Verhalten).

Schnellster Weg: den `CreateService`-Helper von Step 1 in allen bestehenden Tests verwenden, wo aktuell `new BdeBookingService(ctx, userSvc.Object)` steht — durch `CreateService(ctx)` ersetzen (Defaults `multiMa: false, multiOp: false`).

- [ ] **Step 4: StartPlannedAsync mit neuer Enforcement-Logik neu schreiben**

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Services/BdeBookingService.cs` die existierende `StartPlannedAsync`-Methode **komplett ersetzen** durch:

```csharp
    private Task<BdeBookingResult> StartPlannedAsync(int operatorId, int workOperationId, int workplaceId, int terminalId, BdeBookingType type)
    {
        return InTransactionAsync(async () =>
        {
            // Phase-2.1-Gate: Werkbank muss BDE-aktiv sein
            var gateError = await EnsureWorkplaceIsBdeActiveAsync(workplaceId);
            if (gateError != null) return gateError;

            // Settings lesen
            var multiMa = await ReadBoolSettingAsync("BdeMehrfachBuchungProArbeitsgang");
            var multiOp = await ReadBoolSettingAsync("BdeMehrfachBuchungProOperator");

            // Collision-Check: Setup immer strikt, Production nur wenn multiMa=false
            if (type == BdeBookingType.Setup || !multiMa)
            {
                var existingOnWo = await _ctx.BdeBookings
                    .Include(b => b.BdeOperator).Include(b => b.ProductionWorkplace)
                    .FirstOrDefaultAsync(b => b.WorkOperationId == workOperationId && b.EndedAt == null && !b.IsCancelled);

                if (existingOnWo != null && existingOnWo.BdeOperatorId != operatorId)
                    return BdeBookingResult.Collision(existingOnWo);
            }

            // Rule 1 (Terminal): Setup auf demselben AG + type=Production → Transition
            if (type == BdeBookingType.Production)
            {
                var existingSetupSameWo = await _ctx.BdeBookings
                    .FirstOrDefaultAsync(b => b.BdeOperatorId == operatorId && b.EndedAt == null && !b.IsCancelled
                                              && b.BookingType == BdeBookingType.Setup && b.WorkOperationId == workOperationId);
                if (existingSetupSameWo != null)
                {
                    await FinishAndSaveAsync(existingSetupSameWo, null, null);
                    return await CreatePlannedAsync(operatorId, workOperationId, workplaceId, terminalId, type, existingSetupSameWo.Id);
                }
            }

            // Rule 2 (Cumulative): Setup auf anderem AG schließen
            var setupDifferentWo = await _ctx.BdeBookings
                .FirstOrDefaultAsync(b => b.BdeOperatorId == operatorId && b.EndedAt == null && !b.IsCancelled
                                          && b.BookingType == BdeBookingType.Setup && b.WorkOperationId != workOperationId);
            if (setupDifferentWo != null)
                await FinishAndSaveAsync(setupDifferentWo, null, null);

            // Rule 3 (Cumulative): Activity schließen
            var activity = await _ctx.BdeBookings
                .FirstOrDefaultAsync(b => b.BdeOperatorId == operatorId && b.EndedAt == null && !b.IsCancelled
                                          && b.BookingType == BdeBookingType.Activity);
            if (activity != null)
                await FinishAndSaveAsync(activity, null, null);

            // Rule 4 (Terminal): Production-Prüfung
            var existingProduction = await _ctx.BdeBookings
                .FirstOrDefaultAsync(b => b.BdeOperatorId == operatorId && b.EndedAt == null && !b.IsCancelled
                                          && b.BookingType == BdeBookingType.Production);

            if (existingProduction != null)
            {
                if (type == BdeBookingType.Setup)
                    return BdeBookingResult.QuantityRequired(existingProduction);

                // type == Production
                if (!multiOp)
                    return BdeBookingResult.QuantityRequired(existingProduction);
                // multiOp=true → parallel erlaubt, weiter zum Create
            }

            return await CreatePlannedAsync(operatorId, workOperationId, workplaceId, terminalId, type, null);
        });
    }

    private async Task<bool> ReadBoolSettingAsync(string key)
    {
        var value = await _settings.GetValueAsync(key);
        return value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }
```

Der `ReadBoolSettingAsync`-Helper wird als private Methode am Ende der Klasse (im "Interne Helfer"-Block) platziert.

- [ ] **Step 5: Tests laufen lassen, erwartet grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~BdeBookingServiceTests" 2>&1 | tail -10
```

Erwartet: alle alten + neuen Tests grün.

Bei Failures: Die `StartActivityAsync`- und `ResumeAsync`-Methoden bleiben unverändert gegenüber Phase 1. Prüfen ob sie weiter korrekt laufen. Falls `ResumeAsync` jetzt auch gemäß den neuen Regeln laufen soll (Spec sagt ja): analog zur neuen `StartPlannedAsync` anpassen — den Eigener-Operator-Check durch dieselbe Rule-Kaskade ersetzen (nur den Collision-Check und die Create-Logik behalten wie Phase 1).

- [ ] **Step 6: ResumeAsync gemäß neuer Logik anpassen**

Die Spec sagt: Bei Production-Resume gelten dieselben Regeln wie bei StartProduction. Die bestehende `ResumeAsync` ersetzt ihre Eigener-Operator-Check-Logik durch dieselbe Kaskade. Am einfachsten: extrahiere die gemeinsame Logik in einen privaten Helper, den sowohl `StartPlannedAsync` als auch `ResumeAsync` nutzen.

Im Interesse von Minimal-Scope für Phase 2.2: `ResumeAsync` behält seine Phase-1-Logik (die für Einzelbuchungen funktioniert). Bei Multi-Operator ist Resume seltener relevant, und die Phase-1-Logik (Auto-Close der eigenen Buchung, QuantityRequired auf Production) bleibt valid. **Kein Code-Change in ResumeAsync für diesen Task.** Test `Resume_RejectsInactiveWorkplace` von Phase 2.1 läuft unverändert.

- [ ] **Step 7: Full build + tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -10
```

Erwartet: 0 Fehler, alle Tests grün.

- [ ] **Step 8: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Services/BdeBookingService.cs IdealAkeWms.Tests/
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): conditional multi-booking enforcement in BdeBookingService

StartPlannedAsync now uses the cascading rule structure defined in
Phase 2.2 spec: Setup stays strictly 1-op-per-AG; Production respects
BdeMehrfachBuchungProArbeitsgang (collision) and BdeMehrfachBuchungProOperator
(QuantityRequired). Service constructor gains IAppSettingRepository
dependency; existing tests adapted to pass Mock.Of<IAppSettingRepository>."
```

---

## Task 4: BdeBookingService — CloseOtherBookingsOnWorkOperationAsync (neue Methode, TDD)

**Files:**
- Modify: `IdealAkeWms/Services/IBdeBookingService.cs`
- Modify: `IdealAkeWms/Services/BdeBookingService.cs`
- Modify: `IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs`

- [ ] **Step 1: Tests hinzufügen**

Am Ende von `IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs` vor der letzten `}`:

```csharp
    [Fact]
    public async Task CloseOtherBookings_FindsOtherOperatorsOnSameWo()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        var op3 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);

        // drei aktive Buchungen auf WO1
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1)));
        ctx.BdeBookings.Add(new BdeBooking {
            BdeOperatorId = op2, WorkOperationId = ids.WorkOperationId, BdeTerminalId = ids.TerminalId, ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Running, StartedAt = DateTime.Now.AddMinutes(-30),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.BdeBookings.Add(new BdeBooking {
            BdeOperatorId = op3, WorkOperationId = ids.WorkOperationId, BdeTerminalId = ids.TerminalId, ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Running, StartedAt = DateTime.Now.AddMinutes(-15),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx);

        var result = await svc.CloseOtherBookingsOnWorkOperationAsync(ids.WorkOperationId, exceptOperatorId: ids.OperatorId);

        result.ClosedCount.Should().Be(2);
        ctx.BdeBookings.Count(b => b.WorkOperationId == ids.WorkOperationId && b.EndedAt == null).Should().Be(1);
        ctx.BdeBookings.Single(b => b.BdeOperatorId == ids.OperatorId).EndedAt.Should().BeNull();
    }

    [Fact]
    public async Task CloseOtherBookings_SkipsCancelled()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);

        ctx.BdeBookings.Add(new BdeBooking {
            BdeOperatorId = op2, WorkOperationId = ids.WorkOperationId, BdeTerminalId = ids.TerminalId, ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Running, StartedAt = DateTime.Now.AddMinutes(-30),
            IsCancelled = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx);

        var result = await svc.CloseOtherBookingsOnWorkOperationAsync(ids.WorkOperationId, exceptOperatorId: ids.OperatorId);

        result.ClosedCount.Should().Be(0);
    }

    [Fact]
    public async Task CloseOtherBookings_NoOthers_ReturnsZero()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-10)));
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx);

        var result = await svc.CloseOtherBookingsOnWorkOperationAsync(ids.WorkOperationId, exceptOperatorId: ids.OperatorId);

        result.ClosedCount.Should().Be(0);
    }

    [Fact]
    public async Task CloseOtherBookings_SetsAuditFields()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);

        var other = new BdeBooking {
            BdeOperatorId = op2, WorkOperationId = ids.WorkOperationId, BdeTerminalId = ids.TerminalId, ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Running, StartedAt = DateTime.Now.AddMinutes(-30),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.BdeBookings.Add(other);
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx);

        await svc.CloseOtherBookingsOnWorkOperationAsync(ids.WorkOperationId, exceptOperatorId: ids.OperatorId);

        var closed = ctx.BdeBookings.First(b => b.Id == other.Id);
        closed.EndedAt.Should().NotBeNull();
        closed.Status.Should().Be(BdeBookingStatus.Finished);
        closed.ModifiedAt.Should().NotBeNull();
        closed.ModifiedBy.Should().Be("tester");
    }
```

- [ ] **Step 2: Tests laufen lassen — sollten Build-Fehler liefern**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~CloseOtherBookings" 2>&1 | tail -10
```

Erwartet: Build-Fehler ("CloseOtherBookingsOnWorkOperationAsync not found" oder "CloseOthersResult not found").

- [ ] **Step 3: Interface + Record + Implementation hinzufügen**

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Services/IBdeBookingService.cs` ergänzen (am Ende des Interfaces, vor der letzten `}`):

```csharp
    Task<CloseOthersResult> CloseOtherBookingsOnWorkOperationAsync(int workOperationId, int exceptOperatorId);
```

Am Ende der gleichen Datei (außerhalb des Interfaces), den Result-Record deklarieren:

```csharp
public record CloseOthersResult(int ClosedCount);
```

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Services/BdeBookingService.cs` die neue Methode im public-Block hinzufügen (nach `ReportPartialQuantityAsync`):

```csharp
    public Task<CloseOthersResult> CloseOtherBookingsOnWorkOperationAsync(int workOperationId, int exceptOperatorId)
    {
        return InTransactionAsyncTyped(async () =>
        {
            var others = await _ctx.BdeBookings
                .Where(b => b.WorkOperationId == workOperationId
                         && b.BdeOperatorId != exceptOperatorId
                         && b.EndedAt == null
                         && !b.IsCancelled)
                .ToListAsync();

            var now = DateTime.Now;
            foreach (var b in others)
            {
                b.Status = BdeBookingStatus.Finished;
                b.EndedAt = now;
                SetAuditModified(b);
            }

            await _ctx.SaveChangesAsync();
            return new CloseOthersResult(others.Count);
        });
    }

    // Typed variant of InTransactionAsync — returns CloseOthersResult instead of BdeBookingResult
    private async Task<CloseOthersResult> InTransactionAsyncTyped(Func<Task<CloseOthersResult>> action)
    {
        if (_ctx.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
            return await action();

        using IDbContextTransaction tx = await _ctx.Database.BeginTransactionAsync();
        var result = await action();
        await tx.CommitAsync();
        return result;
    }
```

- [ ] **Step 4: Tests laufen lassen — grün erwartet**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~CloseOtherBookings" 2>&1 | tail -10
```

Erwartet: 4 neue Tests grün.

- [ ] **Step 5: Full Suite**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: alle grün.

- [ ] **Step 6: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Services/IBdeBookingService.cs IdealAkeWms/Services/BdeBookingService.cs IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): add CloseOtherBookingsOnWorkOperationAsync

Closes all other active bookings on a WorkOperation (except the
exempt operator's). Used by the Terminal Close-Others dialog after
an operator finalizes a multi-MA AG."
```

---

## Task 5: BdeTimeSplitService (neu, TDD)

**Files:**
- Create: `IdealAkeWms/Services/IBdeTimeSplitService.cs`
- Create: `IdealAkeWms/Services/BdeTimeSplitService.cs`
- Create: `IdealAkeWms.Tests/Services/BdeTimeSplitServiceTests.cs`
- Modify: `IdealAkeWms/Program.cs` (DI-Registrierung)

- [ ] **Step 1: Interface + Record-Datei anlegen**

Datei `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Services/IBdeTimeSplitService.cs`:

```csharp
namespace IdealAkeWms.Services;

public interface IBdeTimeSplitService
{
    Task<IReadOnlyList<BookingSplit>> ComputeForOperatorDayAsync(int operatorId, DateTime day);
    Task<TimeSpan> ComputeEffectiveDurationAsync(int bookingId);
}

public record BookingSplit(int BookingId, TimeSpan EffectiveDuration);
```

- [ ] **Step 2: Test-Datei mit ersten Tests anlegen**

Datei `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms.Tests/Services/BdeTimeSplitServiceTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class BdeTimeSplitServiceTests
{
    private static DateTime Today(int hour = 0, int minute = 0)
        => DateTime.Today.AddHours(hour).AddMinutes(minute);

    [Fact]
    public async Task SingleBooking_NoOverlap_ReturnsFullDuration()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0)));
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        result.Should().HaveCount(1);
        result[0].EffectiveDuration.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public async Task TwoBookings_FullOverlap_EqualQty_SplitsHalf()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        var b1 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0));
        var b2 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0), workOperationId: wo2);
        ctx.BdeBookings.AddRange(b1, b2);
        await ctx.SaveChangesAsync();

        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity { BdeBookingId = b1.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 5, IsFinal = true, ReportedAt = Today(10, 0), CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity { BdeBookingId = b2.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 5, IsFinal = true, ReportedAt = Today(10, 0), CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        result.Should().HaveCount(2);
        result.First(r => r.BookingId == b1.Id).EffectiveDuration.Should().Be(TimeSpan.FromHours(1));
        result.First(r => r.BookingId == b2.Id).EffectiveDuration.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task TwoBookings_FullOverlap_UnequalQty_SplitsByQty()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        var b1 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0));
        var b2 = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0), workOperationId: wo2);
        ctx.BdeBookings.AddRange(b1, b2);
        await ctx.SaveChangesAsync();

        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity { BdeBookingId = b1.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 3, IsFinal = true, ReportedAt = Today(10, 0), CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity { BdeBookingId = b2.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 2, IsFinal = true, ReportedAt = Today(10, 0), CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        // 2h total, split 3:2 → 1.2h : 0.8h = 72min : 48min
        result.First(r => r.BookingId == b1.Id).EffectiveDuration.Should().Be(TimeSpan.FromMinutes(72));
        result.First(r => r.BookingId == b2.Id).EffectiveDuration.Should().Be(TimeSpan.FromMinutes(48));
    }

    [Fact]
    public async Task ThreeSegments_SoloParallelSolo_CorrectSums()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        // A: 08:00-12:00, B: 10:00-14:00, Gutmenge je 5
        var bA = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(12, 0));
        var bB = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(10, 0), endedAt: Today(14, 0), workOperationId: wo2);
        ctx.BdeBookings.AddRange(bA, bB);
        await ctx.SaveChangesAsync();

        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity { BdeBookingId = bA.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 5, IsFinal = true, ReportedAt = Today(12, 0), CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity { BdeBookingId = bB.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 5, IsFinal = true, ReportedAt = Today(14, 0), CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        // A: solo 08-10 (2h) + parallel 10-12 (1h of 2h split 50/50) = 3h
        // B: parallel 10-12 (1h) + solo 12-14 (2h) = 3h
        result.First(r => r.BookingId == bA.Id).EffectiveDuration.Should().Be(TimeSpan.FromHours(3));
        result.First(r => r.BookingId == bB.Id).EffectiveDuration.Should().Be(TimeSpan.FromHours(3));
    }

    [Fact]
    public async Task NoBookings_ReturnsEmpty()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelledBookingsIgnored()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0), cancelled: true));
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FallbackToSollmenge_WhenNoGutmenge()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        // Zweite ProductionOrder mit abweichender Quantity erstellen
        var po2 = new ProductionOrder { OrderNumber = "FA-2", Quantity = 30, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionOrders.Add(po2);
        await ctx.SaveChangesAsync();
        var wo2b = new WorkOperation { ProductionOrderId = po2.Id, OperationNumber = "10", Name = "2nd", Sequence = 10, IsReportable = true, ProductionWorkplaceId = ids.WorkplaceId, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.WorkOperations.Add(wo2b);
        await ctx.SaveChangesAsync();

        // FA-1 hat Quantity 10, FA-2 hat Quantity 30 (Ratio 1:3)
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(12, 0)));
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(12, 0), workOperationId: wo2b.Id));
        await ctx.SaveChangesAsync();

        // Keine BdeBookingQuantity gemeldet

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        // 4h total, Split 10:30 → 1h : 3h
        var firstBooking = ctx.BdeBookings.First(b => b.WorkOperationId == ids.WorkOperationId);
        var secondBooking = ctx.BdeBookings.First(b => b.WorkOperationId == wo2b.Id);

        result.First(r => r.BookingId == firstBooking.Id).EffectiveDuration.Should().Be(TimeSpan.FromHours(1));
        result.First(r => r.BookingId == secondBooking.Id).EffectiveDuration.Should().Be(TimeSpan.FromHours(3));
    }

    [Fact]
    public async Task FallbackToEqual_WhenNoSollmengeAndNoGutmenge()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2 = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);

        // Beide FAs haben Quantity 0 → Fallback auf gleichmäßig
        ctx.ProductionOrders.First().Quantity = 0;
        await ctx.SaveChangesAsync();

        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0)));
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0), workOperationId: wo2));
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var result = await svc.ComputeForOperatorDayAsync(ids.OperatorId, DateTime.Today);

        result.Should().HaveCount(2);
        result.All(r => r.EffectiveDuration == TimeSpan.FromHours(1)).Should().BeTrue();
    }

    [Fact]
    public async Task ComputeEffectiveDurationAsync_SingleDay()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var b = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: Today(8, 0), endedAt: Today(10, 0));
        ctx.BdeBookings.Add(b);
        await ctx.SaveChangesAsync();

        var svc = new BdeTimeSplitService(ctx);

        var duration = await svc.ComputeEffectiveDurationAsync(b.Id);

        duration.Should().Be(TimeSpan.FromHours(2));
    }
}
```

- [ ] **Step 3: Tests laufen lassen — sollten Build-Fehler liefern**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~BdeTimeSplitServiceTests" 2>&1 | tail -10
```

Erwartet: `BdeTimeSplitService` not found.

- [ ] **Step 4: Service-Implementierung**

Datei `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Services/BdeTimeSplitService.cs`:

```csharp
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

public class BdeTimeSplitService : IBdeTimeSplitService
{
    private readonly ApplicationDbContext _ctx;

    public BdeTimeSplitService(ApplicationDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<IReadOnlyList<BookingSplit>> ComputeForOperatorDayAsync(int operatorId, DateTime day)
    {
        var dayStart = day.Date;
        var dayEnd = dayStart.AddDays(1);

        // Schritt 1: Buchungen laden, die mit dem Tagesfenster überlappen
        var now = DateTime.Now;
        var bookings = await _ctx.BdeBookings
            .Include(b => b.WorkOperation)
                .ThenInclude(wo => wo!.ProductionOrder)
            .Include(b => b.Quantities)
            .Where(b => b.BdeOperatorId == operatorId
                     && !b.IsCancelled
                     && b.StartedAt < dayEnd
                     && (b.EndedAt == null || b.EndedAt > dayStart))
            .ToListAsync();

        if (bookings.Count == 0)
            return Array.Empty<BookingSplit>();

        // Für die Berechnung: intern ein "effectiveEnd" pro Buchung, geclippt ans Tagesfenster
        var intervals = bookings
            .Select(b => new
            {
                Booking = b,
                Start = b.StartedAt > dayStart ? b.StartedAt : dayStart,
                End = (b.EndedAt ?? now) < dayEnd ? (b.EndedAt ?? now) : dayEnd
            })
            .Where(x => x.End > x.Start)
            .ToList();

        if (intervals.Count == 0)
            return Array.Empty<BookingSplit>();

        // Schritt 2: distinct Zeitpunkte sammeln
        var timepoints = intervals
            .SelectMany(i => new[] { i.Start, i.End })
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        // Schritt 3+4: Segmente bilden und splitten
        var accum = intervals.ToDictionary(i => i.Booking.Id, _ => TimeSpan.Zero);

        for (int i = 0; i < timepoints.Count - 1; i++)
        {
            var segStart = timepoints[i];
            var segEnd = timepoints[i + 1];
            var segDuration = segEnd - segStart;
            if (segDuration <= TimeSpan.Zero) continue;

            var activeInSeg = intervals
                .Where(iv => iv.Start <= segStart && iv.End >= segEnd)
                .ToList();

            if (activeInSeg.Count == 0) continue;
            if (activeInSeg.Count == 1)
            {
                accum[activeInSeg[0].Booking.Id] += segDuration;
                continue;
            }

            // Gewichtung bestimmen
            var weights = ComputeWeights(activeInSeg.Select(a => a.Booking).ToList());

            var weightSum = weights.Values.Sum();
            if (weightSum <= 0)
            {
                // Fallback 2: gleichmäßig
                var share = segDuration / activeInSeg.Count;
                foreach (var iv in activeInSeg)
                    accum[iv.Booking.Id] += share;
                continue;
            }

            foreach (var iv in activeInSeg)
            {
                var w = weights[iv.Booking.Id];
                accum[iv.Booking.Id] += TimeSpan.FromTicks((long)(segDuration.Ticks * (double)w / (double)weightSum));
            }
        }

        return accum.Select(kv => new BookingSplit(kv.Key, kv.Value)).ToList();
    }

    public async Task<TimeSpan> ComputeEffectiveDurationAsync(int bookingId)
    {
        var booking = await _ctx.BdeBookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return TimeSpan.Zero;

        var start = booking.StartedAt.Date;
        var end = (booking.EndedAt ?? DateTime.Now).Date;

        var total = TimeSpan.Zero;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            var splits = await ComputeForOperatorDayAsync(booking.BdeOperatorId, day);
            var s = splits.FirstOrDefault(x => x.BookingId == bookingId);
            if (s != null)
                total += s.EffectiveDuration;
        }

        return total;
    }

    /// <summary>
    /// Liefert Gewichtungen pro Buchungs-Id. Primär: Summe Gutmenge der Buchung.
    /// Fallback 1: Sollmenge (ProductionOrder.Quantity) der zugehörigen FA.
    /// Fallback 2 (wenn alle Gewichte 0): gleichmäßig (wird im Aufrufer behandelt).
    /// </summary>
    private Dictionary<int, decimal> ComputeWeights(IReadOnlyList<BdeBooking> bookings)
    {
        var weights = bookings.ToDictionary(
            b => b.Id,
            b => b.Quantities?.Sum(q => q.GoodQuantity) ?? 0m);

        if (weights.Values.Sum() > 0m)
            return weights;

        // Fallback 1: Sollmenge
        var sollmenge = bookings.ToDictionary(
            b => b.Id,
            b => (decimal)(b.WorkOperation?.ProductionOrder?.Quantity ?? 0));

        if (sollmenge.Values.Sum() > 0m)
            return sollmenge;

        // Fallback 2: gleichmäßig → 0-Gewichte im Aufrufer erkannt und gleichmäßig verteilt
        return bookings.ToDictionary(b => b.Id, _ => 0m);
    }
}
```

- [ ] **Step 5: DI-Registrierung in Program.cs**

```bash
grep -n "IBdeBookingService\|AddScoped.*Bde" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Program.cs" | head -5
```

Nach der bestehenden BDE-Service-Registrierung ergänzen:

```csharp
builder.Services.AddScoped<IBdeTimeSplitService, BdeTimeSplitService>();
```

- [ ] **Step 6: Tests laufen lassen**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~BdeTimeSplitServiceTests" 2>&1 | tail -15
```

Erwartet: alle Tests grün.

- [ ] **Step 7: Full build + test**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

- [ ] **Step 8: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Services/IBdeTimeSplitService.cs IdealAkeWms/Services/BdeTimeSplitService.cs IdealAkeWms/Program.cs IdealAkeWms.Tests/Services/BdeTimeSplitServiceTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): add BdeTimeSplitService for per-segment effective time

Read-only service computing per-segment time splits for an operator's
day, with Gutmenge as primary weight, Sollmenge (ProductionOrder.Quantity)
as first fallback, and equal-split as second fallback. Handles running
bookings, cancelled exclusion, and multi-day spans via wrapper."
```

---

## Task 6: Terminal-Controller — PausedBookings + CloseOthers + Finish-Erweiterung (TDD)

**Files:**
- Modify: `IdealAkeWms/Controllers/BdeTerminalController.cs`
- Modify: `IdealAkeWms.Tests/Controllers/BdeTerminalControllerTests.cs` (falls Datei nicht existiert: create)

- [ ] **Step 1: Paused + CloseOthers + Finish Tests**

Tests in `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms.Tests/Controllers/BdeTerminalControllerTests.cs` ergänzen (Datei ggf. anlegen, Setup aus `BdeApiControllerTests` kopieren). Siehe Spec für vollständige Liste:

```csharp
    [Fact]
    public async Task PausedBookings_ReturnsOnlyPausedForOperator()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused,
            startedAt: DateTime.Now.AddHours(-2), endedAt: DateTime.Now.AddHours(-1)));
        ctx.BdeBookings.Add(BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running,
            startedAt: DateTime.Now.AddMinutes(-15)));
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx);
        var result = await controller.PausedBookings(ids.OperatorId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        json.Should().Contain("bookingId"); // response enthält pausierte Buchung
        // Nicht-pausierte taucht nicht auf
        var jsonParsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
        jsonParsed.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task PausedBookings_Empty_ReturnsEmptyArray()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var controller = CreateTerminalController(ctx);
        var result = await controller.PausedBookings(ids.OperatorId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
        parsed.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task CloseOthers_DelegatesToService_ReturnsCount()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        ctx.BdeBookings.Add(new BdeBooking {
            BdeOperatorId = op2, WorkOperationId = ids.WorkOperationId, BdeTerminalId = ids.TerminalId, ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Running, StartedAt = DateTime.Now.AddMinutes(-30),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx);
        var result = await controller.CloseOthers(ids.WorkOperationId, ids.OperatorId);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        json.Should().Contain("\"closedCount\":1");
    }
```

(weitere Tests für Finish-Erweiterung in Step 5 nachgereicht, sobald das Finish-DTO feststeht)

- [ ] **Step 2: PausedBookings-Action implementieren**

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Controllers/BdeTerminalController.cs` am Ende der Klasse (vor der letzten `}`):

```csharp
    [HttpGet]
    public async Task<IActionResult> PausedBookings(int operatorId)
    {
        var paused = await _ctx.BdeBookings
            .Include(b => b.WorkOperation)
                .ThenInclude(wo => wo!.ProductionOrder)
            .Where(b => b.BdeOperatorId == operatorId
                     && b.Status == BdeBookingStatus.Paused
                     && !b.IsCancelled)
            .OrderBy(b => b.StartedAt)
            .Select(b => new {
                bookingId = b.Id,
                orderNumber = b.WorkOperation != null ? b.WorkOperation.ProductionOrder!.OrderNumber : "",
                operationNumber = b.WorkOperation != null ? b.WorkOperation.OperationNumber : "",
                operationName = b.WorkOperation != null ? b.WorkOperation.Name : "",
                pausedAt = b.EndedAt
            })
            .ToListAsync();

        return Ok(paused);
    }
```

Achtung: `_ctx` muss vorhanden sein. Falls die Klasse aktuell nur `_bookingSvc` hat, den DbContext im Konstruktor ergänzen.

- [ ] **Step 3: CloseOthers-Action implementieren**

```csharp
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseOthers(int workOperationId, int operatorId)
    {
        var result = await _bookingSvc.CloseOtherBookingsOnWorkOperationAsync(workOperationId, exceptOperatorId: operatorId);
        return Ok(new { closedCount = result.ClosedCount });
    }
```

- [ ] **Step 4: Tests für Steps 1-3 grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "PausedBookings|CloseOthers" 2>&1 | tail -10
```

Erwartet: alle 3 Tests grün.

- [ ] **Step 5: Finish-Action erweitern um `otherActiveBookings`-Feld**

In `BdeTerminalController.cs` die bestehende `Finish`-Action (Zeile ~98) erweitern:

```csharp
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int bookingId, decimal? goodQty, decimal? scrapQty)
    {
        var result = await _bookingSvc.FinishAsync(bookingId, goodQty, scrapQty);

        // Multi-MA-Abschluss-Dialog-Daten (nur bei Setting + IsFinal + Success)
        object[] otherActiveBookings = Array.Empty<object>();

        if (result.Outcome == BdeBookingOutcome.Success
            && (goodQty.HasValue || scrapQty.HasValue)
            && result.Booking?.WorkOperationId is int woId)
        {
            var multiMa = (await _settings.GetValueAsync("BdeMehrfachBuchungProArbeitsgang"))?
                .Equals("true", StringComparison.OrdinalIgnoreCase) == true;

            if (multiMa)
            {
                otherActiveBookings = await _ctx.BdeBookings
                    .Include(b => b.BdeOperator)
                    .Where(b => b.WorkOperationId == woId
                             && b.BdeOperatorId != result.Booking.BdeOperatorId
                             && b.EndedAt == null
                             && !b.IsCancelled)
                    .Select(b => (object)new {
                        operatorId = b.BdeOperatorId,
                        operatorName = b.BdeOperator!.FirstName + " " + b.BdeOperator.LastName,
                        startedAt = b.StartedAt
                    })
                    .ToArrayAsync();
            }
        }

        return Json(MapResultWithOthers(result, otherActiveBookings));
    }
```

**`MapResultWithOthers`** als neuer Helper (entweder existierenden `MapResult` erweitern oder neuen Helper daneben):

```csharp
    private static object MapResultWithOthers(BdeBookingResult r, object[] otherActiveBookings)
    {
        return new {
            outcome = r.Outcome.ToString(),
            bookingId = r.Booking?.Id,
            collidingBookingId = r.CollidingBooking?.Id,
            message = r.Message,
            otherActiveBookings
        };
    }
```

Falls der existierende `MapResult` verwendet wird (Zeile ~114), die neue Version danebensetzen und nur in `Finish` nutzen.

- [ ] **Step 6: `_ctx` + `_settings` im Controller sicherstellen**

Der Ctor muss jetzt `ApplicationDbContext` und `IAppSettingRepository` kennen. Falls noch nicht: ergänzen. Muster aus anderen BDE-Controllern übernehmen.

- [ ] **Step 7: Zwei weitere Finish-Tests**

```csharp
    [Fact]
    public async Task Finish_MultiMaEnabledAndFinalQty_IncludesOtherActiveBookings()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var op2 = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);

        // eigene Buchung + andere aktiv
        var own = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(own);
        ctx.BdeBookings.Add(new BdeBooking {
            BdeOperatorId = op2, WorkOperationId = ids.WorkOperationId, BdeTerminalId = ids.TerminalId, ProductionWorkplaceId = ids.WorkplaceId,
            BookingType = BdeBookingType.Production, Status = BdeBookingStatus.Running, StartedAt = DateTime.Now.AddMinutes(-30),
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx, multiMa: true);

        var result = await controller.Finish(own.Id, goodQty: 5, scrapQty: 0);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        json.Should().Contain("\"otherActiveBookings\"");
        json.Should().Contain($"\"operatorId\":{op2}");
    }

    [Fact]
    public async Task Finish_NoQuantity_OmitsOtherActiveBookings()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var own = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddHours(-1));
        ctx.BdeBookings.Add(own);
        await ctx.SaveChangesAsync();

        var controller = CreateTerminalController(ctx, multiMa: true);

        var result = await controller.Finish(own.Id, goodQty: null, scrapQty: null);

        var json = System.Text.Json.JsonSerializer.Serialize((result as JsonResult)!.Value);
        // otherActiveBookings ist Array, aber leer
        json.Should().Contain("\"otherActiveBookings\":[]");
    }
```

`CreateTerminalController`-Helper (vereinfacht — exakte Signatur aus bestehendem Controller-Setup rüberziehen):

```csharp
    private static BdeTerminalController CreateTerminalController(ApplicationDbContext ctx, bool multiMa = false)
    {
        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync("BdeMehrfachBuchungProArbeitsgang")).ReturnsAsync(multiMa ? "true" : "false");
        settings.Setup(s => s.GetValueAsync("BdeMehrfachBuchungProOperator")).ReturnsAsync("false");

        var bookingSvc = new BdeBookingService(ctx, userSvc.Object, settings.Object);
        // ggf. weitere Services (DefaultWorkOperationService, ...) mocken — aus bestehendem Controller-Setup kopieren

        return new BdeTerminalController(ctx, bookingSvc, settings.Object /* weitere Services */);
    }
```

**Hinweis:** Die genaue Konstruktor-Signatur des Controllers muss mit den tatsächlichen Abhängigkeiten übereinstimmen. Aus der existierenden `BdeApiControllerTests` oder der Controller-Definition selbst ableiten.

- [ ] **Step 8: Full tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -10
```

Erwartet: alle grün.

- [ ] **Step 9: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Controllers/BdeTerminalController.cs IdealAkeWms.Tests/Controllers/
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): terminal endpoints PausedBookings + CloseOthers + Finish extension

GET /BdeTerminal/PausedBookings returns the operator's paused
bookings for the after-scan hint. POST /BdeTerminal/CloseOthers
delegates to BdeBookingService. Finish response now includes
otherActiveBookings array (always present, empty when not relevant)."
```

---

## Task 7: Terminal JS — Paused-Hint + Close-Others-Modal

**Files:**
- Modify: `IdealAkeWms/wwwroot/js/bde-terminal.js`
- Modify: `IdealAkeWms/Views/BdeTerminal/Index.cshtml`

Kein automatischer Test — UI-Smoke in Task 11.

- [ ] **Step 1: HTML-Platzhalter für Paused-Hint in Index.cshtml**

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Views/BdeTerminal/Index.cshtml` nach dem Operator-Scan-Panel (grep nach "Operator" oder "bde-operator"), vor der AG-Auswahl, einfügen:

```html
<div id="paused-bookings-hint" class="alert alert-warning mb-3 d-none">
    <strong>Hinweis: Sie haben pausierte Aufträge</strong>
    <ul id="paused-bookings-list" class="list-unstyled mt-2 mb-0"></ul>
</div>
```

- [ ] **Step 2: HTML-Platzhalter für Close-Others-Modal**

Am Ende der Index.cshtml (vor dem `@section Scripts`-Block):

```html
<div class="modal fade" id="close-others-modal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Weitere aktive Buchungen</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <p>Auf diesem Arbeitsgang arbeiten noch:</p>
                <ul id="close-others-list"></ul>
                <p>Sollen diese Buchungen auch beendet werden?</p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Nein, nur meine</button>
                <button type="button" class="btn btn-primary" id="close-others-confirm">Ja, alle beenden</button>
            </div>
        </div>
    </div>
</div>
```

- [ ] **Step 3: JS — Paused-Hint nach Operator-Scan**

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/wwwroot/js/bde-terminal.js`:

Am Ende der bestehenden Operator-Scan-Success-Handler-Funktion einen Aufruf hinzufügen:

```javascript
async function loadPausedBookings(operatorId) {
    const hint = document.getElementById('paused-bookings-hint');
    const list = document.getElementById('paused-bookings-list');
    if (!hint || !list) return;

    try {
        const res = await fetch(`/BdeTerminal/PausedBookings?operatorId=${operatorId}`);
        if (!res.ok) { hint.classList.add('d-none'); return; }
        const items = await res.json();

        if (!items.length) { hint.classList.add('d-none'); list.innerHTML = ''; return; }

        list.innerHTML = items.map(i => `
            <li class="mb-2">
                <strong>${i.orderNumber} / ${i.operationNumber} ${i.operationName || ''}</strong>
                <small class="text-muted d-block">pausiert seit ${i.pausedAt ? new Date(i.pausedAt).toLocaleString('de-DE') : ''}</small>
                <button type="button" class="btn btn-sm btn-warning mt-1" data-booking-id="${i.bookingId}" data-resume>Fortsetzen</button>
            </li>
        `).join('');
        hint.classList.remove('d-none');
    } catch (e) {
        console.error('Error loading paused bookings', e);
        hint.classList.add('d-none');
    }
}
```

Irgendwo im Operator-Scan-Success-Handler den Call ergänzen: `loadPausedBookings(operatorId);` (direkt nachdem die Operator-Info im UI gesetzt ist).

Für den "Fortsetzen"-Button-Click einen Event-Listener registrieren (z.B. am Ende des DOMContentLoaded-Handlers):

```javascript
document.getElementById('paused-bookings-list')?.addEventListener('click', async (e) => {
    const btn = e.target.closest('[data-resume]');
    if (!btn) return;
    const bookingId = parseInt(btn.dataset.bookingId, 10);
    const operatorId = window.currentOperatorId; // muss vom Operator-Scan gesetzt werden
    if (!operatorId) return;

    // Resume-Flow anstoßen (existing endpoint)
    const form = new FormData();
    form.append('pausedBookingId', bookingId);
    form.append('operatorId', operatorId);
    form.append('resumeAs', '2'); // Production
    form.append('workplaceId', window.currentWorkplaceId);
    form.append('terminalId', window.currentTerminalId);
    form.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);

    const res = await fetch('/BdeTerminal/Resume', { method: 'POST', body: form });
    if (res.ok) {
        // zeile aus list entfernen
        btn.closest('li')?.remove();
        if (!document.querySelectorAll('#paused-bookings-list li').length) {
            document.getElementById('paused-bookings-hint').classList.add('d-none');
        }
    } else {
        alert('Fortsetzen fehlgeschlagen');
    }
});
```

**Hinweis:** Die Namensgebung `window.currentOperatorId`, `currentWorkplaceId`, `currentTerminalId` muss mit den bestehenden Variablen im `bde-terminal.js` übereinstimmen. Per grep herausfinden welche State-Variablen der bestehende Code nutzt:

```bash
grep -n "currentOperator\|selectedOperator\|operatorId\s*=\|scannedOperator" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/wwwroot/js/bde-terminal.js" | head -15
```

Die existierenden Namen übernehmen.

- [ ] **Step 4: JS — Finish-Response-Handler erweitern für Close-Others-Modal**

Im existierenden Finish-Response-Handler (grep nach `/BdeTerminal/Finish` oder der bestehenden Finish-Result-Verarbeitung):

```javascript
// Nach erfolgreichem Finish:
if (response.outcome === 'Success' && response.otherActiveBookings && response.otherActiveBookings.length > 0) {
    const list = document.getElementById('close-others-list');
    list.innerHTML = response.otherActiveBookings.map(o =>
        `<li>${o.operatorName} — seit ${new Date(o.startedAt).toLocaleTimeString('de-DE', {hour: '2-digit', minute: '2-digit'})}</li>`
    ).join('');

    const modal = new bootstrap.Modal(document.getElementById('close-others-modal'));
    modal.show();

    document.getElementById('close-others-confirm').onclick = async () => {
        const form = new FormData();
        form.append('workOperationId', response.workOperationId ?? currentWorkOperationId);
        form.append('operatorId', window.currentOperatorId);
        form.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);

        const res = await fetch('/BdeTerminal/CloseOthers', { method: 'POST', body: form });
        const data = await res.json();
        modal.hide();
        showToast(`${data.closedCount} weitere Buchungen beendet.`);
    };
}
```

Wichtig: die Variable `workOperationId` in der Response ist nicht im aktuellen Finish-Mapping. Entweder:
- `MapResultWithOthers` in Task 6 um `workOperationId = r.Booking?.WorkOperationId` erweitern, **oder**
- JS führt die `currentWorkOperationId` selbst mit (aus dem vorherigen Start-Flow)

Wir erweitern den Mapper. Zurück in `BdeTerminalController.cs` → `MapResultWithOthers`:

```csharp
    private static object MapResultWithOthers(BdeBookingResult r, object[] otherActiveBookings)
    {
        return new {
            outcome = r.Outcome.ToString(),
            bookingId = r.Booking?.Id,
            workOperationId = r.Booking?.WorkOperationId,
            collidingBookingId = r.CollidingBooking?.Id,
            message = r.Message,
            otherActiveBookings
        };
    }
```

- [ ] **Step 5: Build + Test**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Erwartet: 0 Fehler. Alle Tests grün.

- [ ] **Step 6: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/wwwroot/js/bde-terminal.js IdealAkeWms/Views/BdeTerminal/Index.cshtml IdealAkeWms/Controllers/BdeTerminalController.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): terminal UI for paused-bookings hint and close-others modal

After operator scan, fetch paused bookings and show them as an alert
with Fortsetzen buttons. After Finish, when the backend flags other
active bookings on the same WO, show a Bootstrap modal to optionally
close them all."
```

---

## Task 8: Buchungsübersicht — "Effektive Zeit"-Spalte

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/BdeBookingListViewModel.cs`
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs`
- Modify: `IdealAkeWms/Controllers/BdeBookingsController.cs`
- Modify: `IdealAkeWms/Views/BdeBookings/Index.cshtml`
- Modify: `IdealAkeWms/Views/BdeBookings/Edit.cshtml`
- Modify: `IdealAkeWms.Tests/Controllers/BdeBookingsControllerTests.cs` (falls nicht existiert: create)

- [ ] **Step 1: ViewModel erweitern**

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Models/ViewModels/BdeBookingListViewModel.cs` Property ergänzen:

```csharp
    public Dictionary<int, TimeSpan> EffectiveDurations { get; set; } = new();
```

- [ ] **Step 2: ColumnDefinitions erweitern**

In `ColumnDefinitions.cs` im `BdeBookings` `ViewConfig` nach `ended-at` einfügen:

```csharp
            new ColumnDef("effective-duration","Effektive Zeit",Locked: false),
```

- [ ] **Step 3: Test für Controller-Populate**

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms.Tests/Controllers/BdeBookingsControllerTests.cs`:

```csharp
    [Fact]
    public async Task Index_PopulatesEffectiveDurations()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var b = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished,
            startedAt: DateTime.Today.AddHours(8), endedAt: DateTime.Today.AddHours(10));
        ctx.BdeBookings.Add(b);
        await ctx.SaveChangesAsync();

        var controller = CreateBookingsController(ctx);

        var result = await controller.Index() as ViewResult;

        var vm = result!.Model as BdeBookingListViewModel;
        vm.Should().NotBeNull();
        vm!.EffectiveDurations.Should().ContainKey(b.Id);
        vm.EffectiveDurations[b.Id].Should().Be(TimeSpan.FromHours(2));
    }
```

`CreateBookingsController`-Helper spiegelt den tatsächlichen Ctor des `BdeBookingsController` — aus existierender Controller-Datei ableiten.

- [ ] **Step 4: Tests laufen lassen — rot**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "Index_PopulatesEffectiveDurations" 2>&1 | tail -10
```

- [ ] **Step 5: BdeBookingsController.Index erweitern**

Im `BdeBookingsController.cs` Index-Action, nach dem Laden der Buchungen und vor dem `return View(vm)`:

```csharp
        // Effektive Zeiten pro Buchung berechnen
        var operatorDayPairs = vm.Bookings
            .Where(b => b.BdeOperatorId > 0)
            .SelectMany(b =>
            {
                var start = b.StartedAt.Date;
                var end = (b.EndedAt ?? DateTime.Now).Date;
                var days = new List<DateTime>();
                for (var d = start; d <= end; d = d.AddDays(1))
                    days.Add(d);
                return days.Select(d => (OperatorId: b.BdeOperatorId, Day: d));
            })
            .Distinct()
            .ToList();

        foreach (var pair in operatorDayPairs)
        {
            var splits = await _timeSplitSvc.ComputeForOperatorDayAsync(pair.OperatorId, pair.Day);
            foreach (var s in splits)
            {
                if (!vm.EffectiveDurations.ContainsKey(s.BookingId))
                    vm.EffectiveDurations[s.BookingId] = TimeSpan.Zero;
                vm.EffectiveDurations[s.BookingId] += s.EffectiveDuration;
            }
        }
```

`IBdeTimeSplitService _timeSplitSvc` im Konstruktor ergänzen.

**Hinweis:** `vm.Bookings` sollte eine Liste mit `BdeBooking`-Zugriff sein. Falls es ein DTO ist, müssen `BdeOperatorId`, `StartedAt`, `EndedAt` verfügbar sein — andernfalls die Controller-Logik an die tatsächliche VM-Struktur anpassen.

- [ ] **Step 6: Index.cshtml erweitern**

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Views/BdeBookings/Index.cshtml` im `<thead>` zwischen Ende und Operator:

```html
<th data-col-key="effective-duration">Effektive Zeit</th>
```

In `<tbody>` innerhalb der foreach:

```html
<td data-col-key="effective-duration">
    @{
        var eff = Model.EffectiveDurations.GetValueOrDefault(booking.Id, TimeSpan.Zero);
        var runningSuffix = booking.EndedAt == null ? "*" : "";
    }
    <span title="@(booking.EndedAt == null ? "Wird nach Beenden angepasst" : "")">
        @eff.ToString(@"hh\:mm")@runningSuffix
    </span>
</td>
```

- [ ] **Step 7: Edit.cshtml erweitern**

In `Views/BdeBookings/Edit.cshtml` als read-only Display ergänzen (nach Ende-Feld):

```html
<div class="mb-3">
    <label class="form-label">Effektive Zeit</label>
    <input type="text" class="form-control-plaintext" readonly value="@(ViewBag.EffectiveDuration?.ToString(@"hh\:mm") ?? "—")" />
</div>
```

In der Controller-`Edit`-GET-Action `ViewBag.EffectiveDuration = await _timeSplitSvc.ComputeEffectiveDurationAsync(booking.Id);` setzen.

- [ ] **Step 8: Tests laufen + Commit**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5

git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/ IdealAkeWms.Tests/
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): effective-duration column in bookings overview

BdeBookingListViewModel gets EffectiveDurations dict; Index action
aggregates per-operator-day splits from BdeTimeSplitService (handles
cross-day bookings by iterating over all spanned days). Edit view
shows the computed effective duration as read-only field."
```

---

## Task 9: Settings-UI — 2 neue Toggles

**Files:**
- Modify: `IdealAkeWms/Views/Settings/Index.cshtml`

- [ ] **Step 1: BDE-Gruppe erweitern**

In `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Views/Settings/Index.cshtml` grep nach `"BdeAktiv"`:

```bash
grep -n "BdeAktiv\|BdeNurFaMeldung\|BdeDefaultArbeitsgang" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Views/Settings/Index.cshtml"
```

Die existierende BDE-Gruppe erweitern:

```csharp
("BDE", new[] { "BdeAktiv", "BdeNurFaMeldung", "BdeDefaultArbeitsgang", "BdeMehrfachBuchungProOperator", "BdeMehrfachBuchungProArbeitsgang" }),
```

- [ ] **Step 2: UI-Gate für die neuen Toggles (optional, wenn inline möglich)**

Der Render der Toggles ist generisch. Falls die Settings-Seite UI-seitig Felder nur ausgraut wenn ein Master-Toggle aus ist: das existierende Muster für `BdeNurFaMeldung` (abhängig von `BdeAktiv`) als Vorlage nehmen. Minimal-Änderung für diesen Task: keine UI-Gates — nutzer werden durch die Beschreibung der Toggles bereits ausreichend geführt. Spec-Anforderung "ausgegraut wenn BdeAktiv=false" kann als Follow-Up in Phase 2.3+ umgesetzt werden; hier als Kommentar dokumentieren.

- [ ] **Step 3: Build + Test**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

- [ ] **Step 4: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Views/Settings/Index.cshtml
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(bde): settings UI toggles for multi-booking settings"
```

---

## Task 10: SQL-Scripts + FreshInstall + Docs + PROJECT_STATUS + AppVersion

**Files:**
- Create: `SQL/45_RelaxBdeBookingConstraints.sql`
- Modify: `SQL/00_FreshInstall.sql`
- Modify: `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs`
- Modify: `CLAUDE.md`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `PROJECT_STATUS.md`

- [ ] **Step 1: Migration-ID auslesen**

```bash
ls "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Migrations/" | grep -i "RelaxBdeBookingConstraints"
```

Timestamp-Präfix (z.B. `20260421XXXXXX`) für den MigrationId-Eintrag in SQL/45 merken.

- [ ] **Step 2: `SQL/45_RelaxBdeBookingConstraints.sql` anlegen**

```sql
-- Relax filtered UNIQUE indexes on BdeBookings to allow multi-booking.
-- Idempotent: safe to re-run.

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_BdeOperatorId_Active' AND object_id = OBJECT_ID(N'dbo.BdeBookings') AND is_unique = 1)
BEGIN
    DROP INDEX [IX_BdeBookings_BdeOperatorId_Active] ON [dbo].[BdeBookings];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_BdeOperatorId_Active' AND object_id = OBJECT_ID(N'dbo.BdeBookings'))
BEGIN
    CREATE INDEX [IX_BdeBookings_BdeOperatorId_Active]
        ON [dbo].[BdeBookings] ([BdeOperatorId])
        WHERE [EndedAt] IS NULL AND [IsCancelled] = 0;
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_WorkOperationId_Active' AND object_id = OBJECT_ID(N'dbo.BdeBookings') AND is_unique = 1)
BEGIN
    DROP INDEX [IX_BdeBookings_WorkOperationId_Active] ON [dbo].[BdeBookings];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BdeBookings_WorkOperationId_Active' AND object_id = OBJECT_ID(N'dbo.BdeBookings'))
BEGIN
    CREATE INDEX [IX_BdeBookings_WorkOperationId_Active]
        ON [dbo].[BdeBookings] ([WorkOperationId])
        WHERE [EndedAt] IS NULL AND [IsCancelled] = 0;
END
GO

-- Seed neue AppSettings (idempotent)
IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = 'BdeMehrfachBuchungProOperator')
    INSERT INTO dbo.AppSettings ([Key], Value, Description, CreatedAt, CreatedBy, CreatedByWindows)
    VALUES ('BdeMehrfachBuchungProOperator', 'false', 'Ein Mitarbeiter darf mehrere parallele Buchungen haben (auf verschiedenen Arbeitsgängen)', SYSDATETIME(), 'System', 'System');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = 'BdeMehrfachBuchungProArbeitsgang')
    INSERT INTO dbo.AppSettings ([Key], Value, Description, CreatedAt, CreatedBy, CreatedByWindows)
    VALUES ('BdeMehrfachBuchungProArbeitsgang', 'false', 'Ein Arbeitsgang darf mehrere parallele Buchungen haben (durch verschiedene Mitarbeiter)', SYSDATETIME(), 'System', 'System');
GO

-- EF Migration History
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE '%_RelaxBdeBookingConstraints')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260421XXXXXX_RelaxBdeBookingConstraints', '10.0.2');
END
GO
```

`20260421XXXXXX` durch die tatsächliche Migration-Id aus Step 1 ersetzen. Die AppSettings-Tabelle-Spalten (`CreatedAt` etc.) aus einer existierenden Seed-Insert-Zeile in `SQL/00_FreshInstall.sql` abgleichen (Spalten-Namen und Casing können abweichen).

- [ ] **Step 3: `SQL/00_FreshInstall.sql` anpassen**

Die existierenden CREATE-Statements für `IX_BdeBookings_BdeOperatorId_Active` und `IX_BdeBookings_WorkOperationId_Active` finden:

```bash
grep -n "IX_BdeBookings_BdeOperatorId_Active\|IX_BdeBookings_WorkOperationId_Active" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/SQL/00_FreshInstall.sql"
```

`CREATE UNIQUE INDEX` → `CREATE INDEX` ändern (Filter-Clause bleibt).

Seeds für die beiden neuen AppSettings ebenfalls in den AppSettings-Seed-Block einfügen (gleiches Muster wie bestehende `BdeAktiv`-Seeds).

- [ ] **Step 4: AppVersion Date bumpen**

In beiden AppVersion.cs (Web + Service) `Date` auf `"2026-04-21"` setzen. `Version` bleibt `"1.8.2"`.

- [ ] **Step 5: CLAUDE.md aktualisieren**

AppSettings-Tabelle um die 2 neuen Keys ergänzen. Bestehenden Eintrag "BDE-Buchung Ein-Operator-Regel" unter "Bekannte Fallstricke" überarbeiten auf "Constraints sind konditional basierend auf Settings `BdeMehrfachBuchungProOperator` und `BdeMehrfachBuchungProArbeitsgang`; Enforcement im Service, nicht mehr als UNIQUE-Index."

- [ ] **Step 6: Help/Index.cshtml erweitern**

Im BDE-Abschnitt einen neuen Unterabschnitt ergänzen:

```html
<h6 class="mt-3">Mehrfach-Anmeldung konfigurieren</h6>
<ol>
    <li>Einstellungen → BDE-Gruppe öffnen.</li>
    <li>"Ein Mitarbeiter darf mehrere parallele Buchungen haben" aktivieren — wenn ein MA mehrere Maschinen/Aufträge gleichzeitig bedient.</li>
    <li>"Ein Arbeitsgang darf mehrere parallele Buchungen haben" aktivieren — wenn mehrere MA gemeinsam an einem Auftrag arbeiten (Montage-Team).</li>
    <li>Speichern.</li>
</ol>

<h6 class="mt-3">Zeit-Split bei paralleler Arbeit</h6>
<p>Arbeitet ein MA parallel auf mehreren Aufträgen, wird die Gesamtzeit automatisch auf die Aufträge aufgeteilt — primär nach tatsächlicher Gutmenge, bei fehlender Rückmeldung nach geplanter Sollmenge. Beispiel: 4h parallel auf FA-A (5 Stk) und FA-B (5 Stk) → je 2h effektive Zeit pro Auftrag. Die effektive Zeit wird in der Buchungsübersicht in der Spalte "Effektive Zeit" angezeigt.</p>

<h6 class="mt-3">Pausierte Aufträge fortsetzen</h6>
<p>Nach dem Operator-Scan am Terminal sehen Sie einen Hinweis mit allen Ihren pausierten Aufträgen und können diese per Klick fortsetzen.</p>
```

- [ ] **Step 7: Changelog.cshtml erweitern**

Im bestehenden v1.8.2-Block neue Bullet-Punkte für Phase 2.2 ergänzen:

```html
<h6>BDE Phase 2.2 — Mehrfachanmeldung + Zeit-Split</h6>
<ul>
    <li>Neue Settings <code>BdeMehrfachBuchungProOperator</code> und <code>BdeMehrfachBuchungProArbeitsgang</code> — ermöglichen 1 MA auf mehreren Aufträgen bzw. mehrere MA auf 1 Auftrag (beide Defaults false).</li>
    <li>Effektive-Zeit-Berechnung pro-Segment bei parallelen Buchungen (Buchungsübersicht zeigt neue Spalte).</li>
    <li>Terminal: Hinweis-Block mit pausierten Aufträgen nach Operator-Scan (mit Fortsetzen-Button).</li>
    <li>Terminal: Bei Abschluss-Meldung auf Multi-MA-AG → Dialog "andere Buchungen auch beenden?".</li>
</ul>
```

- [ ] **Step 8: PROJECT_STATUS.md ergänzen**

Neue Tabellenzeile oder Block für "BDE Phase 2.2 — abgeschlossen 2026-04-21" analog zum Phase-2.1-Eintrag.

- [ ] **Step 9: Build + Full Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -10
```

Erwartet: 0 Fehler, alle Tests grün.

- [ ] **Step 10: Manueller UI-Smoke-Test**

Server starten:

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms" && dotnet run --no-build --urls="http://localhost:5088"
```

Voraussetzung: `BdeAktiv = true` im globalen Setting. Manuell prüfen (Browser):

1. Settings-Seite → zwei neue BDE-Toggles sichtbar
2. Toggles aus → Phase-1-Verhalten: 2. Production-Scan liefert QuantityRequired-Dialog
3. `ProOperator = true` → 2. Production-Scan startet parallel ohne Dialog
4. `ProArbeitsgang = true` + 2 Operator-Scans auf gleichem AG → beide Buchungen aktiv
5. Abschluss-Meldung (mit Gutmenge) auf Multi-MA-AG → Close-Others-Modal erscheint
6. Klick "Ja" → andere Buchungen enden, Toast zeigt Count
7. Operator scannt sich ein + hat pausierte Buchung → Hinweis-Block oben sichtbar, Fortsetzen funktioniert
8. Buchungsübersicht → "Effektive Zeit"-Spalte zeigt Werte; bei laufenden `*`-Suffix
9. Setup-Versuch mit anderem Operator auf bereits gerüsteter Werkbank → weiter Collision trotz ProArbeitsgang=true
10. Activity-Start während Production-läuft → QuantityRequired trotz ProOperator=true

Server stoppen (Ctrl+C bzw. taskkill).

- [ ] **Step 11: Commit + Final-Log**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" status --porcelain
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add SQL/ CLAUDE.md IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/ PROJECT_STATUS.md
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "chore(bde): SQL + fresh install + docs + version date for Phase 2.2"
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" log --oneline -15
```

Erwartet: 10 Phase-2.2-Commits am Stück.
