# FA-Reconciliation (verwaiste Produktionsaufträge stornieren) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Follow TDD: write the failing test first, run it red, implement, run it green, commit.

**Goal:** Beim Sage-FA-Sync erkennen, welche in der WMS **offenen** Produktionsaufträge (FAs) **nicht mehr in Sage** sind, und sie **stornieren** (neues Flag `IsCancelled`). Stornierte FAs verschwinden aus allen offenen Sichten; taucht eine wieder in Sage auf → **auto-reaktivieren**. Sicherheits-Guard (leerer Sage-Read) + Cap (max. Storni pro Lauf) verhindern versehentliches Massen-Stornieren bei Sage-Aussetzern. Opt-in per Flag (Default AUS), DryRun-fähig.

**Architecture:** Drei additive Spalten an `ProductionOrder` (`IsCancelled`/`CancelledAt`/`CancelledBy`) via Migration 80. Die Reconcile-Entscheidung liegt in einem **reinen, voll unit-testbaren Helper** `ProductionOrderReconciler.Plan(...)` (keine DB). Verdrahtung in `SageImportService.SyncProductionOrdersAsync` NACH dem Upsert, im selben SyncLog-Run — unter dem Flag `Sync:ProductionOrderReconcileEnabled`. Alle Offen-Queries (EF-Repo + zwei Service-Kandidaten-Queries + BomCache-raw-SQL) bekommen zusätzlich `!IsCancelled`. Der Cap-Skip löst eine Fehlermail über den vorhandenen `SyncErrorNotifier` aus.

**Tech Stack:** ASP.NET Core 10 MVC (`IdealAkeWms/`) + EF Core 10 (SQL Server) für den Web-Teil; .NET 10 Windows-Service (`IDEALAKEWMSService`) mit raw ADO.NET + EF Core für den Sync. Tests: xUnit + FluentAssertions + Moq + EF InMemory (`TestDbContextFactory.Create()`), Service-Fakes (`FakeSyncLogger`).

**Branch / Worktree:** `feature/glas-bestellung` im Worktree `.claude/worktrees/glas-bestellung` (HEAD `44d1f21`). Spec: [docs/superpowers/specs/2026-07-07-fa-reconciliation-design.md](../specs/2026-07-07-fa-reconciliation-design.md).

**WICHTIG (Standing Constraints):**
- KEIN Merge nach `main`, KEIN Worktree-/Branch-Cleanup ohne ausdrückliche User-Freigabe. Plan endet mit grünem Build + Tests auf dem Branch.
- **KEIN AppVersion-Bump** — dieses Feature wird in das noch unreleaste **v1.25.0** gefaltet. `AppVersion.cs` (Web + Service) bleibt unberührt; nur die bestehende v1.25.0-Changelog-Card wird erweitert.
- Alle git-Befehle laufen im Worktree: `cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git ...`.
- Commit je Task mit Trailer `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

**Build / Test-Befehle (immer aus dem Worktree-Root):**
- Build: `dotnet build IdealAkeWms.slnx`
- Web-Tests: `dotnet test IdealAkeWms.Tests --nologo`
- Service-Tests: `dotnet test IDEALAKEWMSService.Tests --nologo`

---

## File Structure

**Neu:**
- `IDEALAKEWMSService/Services/ProductionOrderReconciler.cs` — reiner Helper (`record WmsOrderState`, `record ReconcilePlan`, `static Plan(...)`).
- `IDEALAKEWMSService.Tests/Services/ProductionOrderReconcilerTests.cs` — Unit-Tests dazu.
- `IdealAkeWms/Migrations/<timestamp>_AddProductionOrderCancellation.cs` (+ `.Designer.cs`, generiert) — EF-Migration.
- `SQL/80_AddProductionOrderCancellation.sql` — idempotentes Deploy-Skript.

**Geändert:**
- `IdealAkeWms/Models/ProductionOrder.cs` — 3 Felder.
- `IdealAkeWms/Data/ApplicationDbContext.cs` — `CancelledBy`-MaxLength(256) + `IsCancelled`-Index (optional/empfohlen).
- `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs` — 3 Offen-Queries.
- `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs` — `LeitstandOrderRow` um `IsCancelled` erweitern.
- `IdealAkeWms/Controllers/ProductionOrdersController.cs` — Badge-Feld ins ViewItem.
- `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs` — `IsCancelled` an `ProductionOrderListItem`.
- `IdealAkeWms/Controllers/FaCompletionController.cs`, `IdealAkeWms/Controllers/FaWorklistController.cs` — `!IsCancelled` im Offen-Filter.
- `IdealAkeWms/Views/ProductionOrders/Index.cshtml` — Badge „In Sage gelöscht".
- `IDEALAKEWMSService/Services/BomCacheSyncService.cs` — raw-SQL `AND IsCancelled = 0`.
- `IDEALAKEWMSService/Services/FaWorkStepDetectionService.cs` — beide Kandidaten-Queries `&& !o.IsCancelled`.
- `IDEALAKEWMSService/Services/SageImportService.cs` — Reconcile-Verdrahtung (+ `ISyncErrorNotifier`-Injektion).
- `IDEALAKEWMSService.Tests/Services/SageImportServiceTests.cs` — Konstruktor-Aufruf um Notifier-Mock ergänzen.
- `IDEALAKEWMSService/appsettings.json` — 2 neue `Sync:`-Keys.
- `SQL/00_FreshInstall.sql` — ProductionOrders-Schema-Block (3 Spalten) + History-Insert.
- `IdealAkeWms/Views/Help/Changelog.cshtml`, `CLAUDE.md`, `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md` — Doku.

---

## Task 0: Pre-Flight — Baseline grün, HEAD bestätigen

**Files:** keine (nur Verifikation).

- [ ] **Step 1: HEAD bestätigen**

Run: `cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git rev-parse --short HEAD && git status --short`
Expected: HEAD `44d1f21` (oder neuer, falgs zwischenzeitlich commits). Working tree sauber (bis auf ggf. diesen Plan).

- [ ] **Step 2: Build grün**

Run: `dotnet build IdealAkeWms.slnx`
Expected: `Build succeeded.` mit `0 Error(s)`.

- [ ] **Step 3: Web + Service-Tests grün**

Run: `dotnet test IdealAkeWms.Tests --nologo`
Expected: `Passed!` (alle grün — Baseline v1.25.0 ist grün, ~839 Web-Tests).

Run: `dotnet test IDEALAKEWMSService.Tests --nologo`
Expected: `Passed!` (alle grün, ~115 Service-Tests).

> Falls hier etwas rot ist, STOPP — nicht mit einem roten Baseline weiterarbeiten.

---

## Task 1: `ProductionOrder` — 3 Stornierungs-Felder

**Files:**
- Modify: `IdealAkeWms/Models/ProductionOrder.cs:52` (nach den Nav-Properties)
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs:376` (ProductionOrder-Config)

- [ ] **Step 1: Felder ans Modell**

In `IdealAkeWms/Models/ProductionOrder.cs` NACH Zeile 51 (`public ProductionOrderBdeStatus? BdeStatus { get; set; }`) und VOR der schließenden Klammer einfügen:

```csharp

    // FA-Reconciliation (v1.25.0): verwaiste FAs, die in Sage geloescht wurden.
    // IsCancelled verhaelt sich in allen Offen-Queries wie IsDone (raus aus offenen Sichten).
    [Display(Name = "Storniert")]
    public bool IsCancelled { get; set; }

    [Display(Name = "Storniert am")]
    public DateTime? CancelledAt { get; set; }

    [StringLength(256)]
    [Display(Name = "Storniert von")]
    public string? CancelledBy { get; set; }
```

- [ ] **Step 2: EF-Config (MaxLength + Index)**

In `IdealAkeWms/Data/ApplicationDbContext.cs` im `ProductionOrder`-Block NACH Zeile 372 (`entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);`) einfügen:

```csharp
            entity.Property(e => e.CancelledBy).HasMaxLength(256);
```

Und NACH Zeile 376 (`entity.HasIndex(e => e.IsDone);`) einfügen:

```csharp
            entity.HasIndex(e => e.IsCancelled);
```

> `bool`/`DateTime?` brauchen keine Extra-Config (EF-Defaults reichen: `bit NOT NULL`, `datetime2 NULL`). `IsCancelled` erhält Default `false` automatisch (nicht-nullable bool). Der Index unterstützt die künftigen `!IsCancelled`-Filter.

- [ ] **Step 3: Build**

Run: `dotnet build IdealAkeWms.slnx`
Expected: `Build succeeded.` `0 Error(s)`. (Es erscheint ggf. die EF-Warnung `PendingModelChangesWarning` erst zur Laufzeit/Migration — Build selbst ist grün.)

- [ ] **Step 4: Commit**

Run:
```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Models/ProductionOrder.cs IdealAkeWms/Data/ApplicationDbContext.cs && git commit -m "$(cat <<'EOF'
feat(model): ProductionOrder.IsCancelled/CancelledAt/CancelledBy (FA-Reconciliation v1.25.0)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Migration 80 + `SQL/80` + FreshInstall

**Files:**
- Create: `IdealAkeWms/Migrations/<timestamp>_AddProductionOrderCancellation.cs` (EF-generiert)
- Create: `SQL/80_AddProductionOrderCancellation.sql`
- Modify: `SQL/00_FreshInstall.sql:222` (ProductionOrders-CREATE) + `:2081` (History-Insert-Block)

- [ ] **Step 1: EF-Migration generieren**

Run:
```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet ef migrations add AddProductionOrderCancellation --project IdealAkeWms
```
Expected: `Done.` — es entsteht `IdealAkeWms/Migrations/<timestamp>_AddProductionOrderCancellation.cs` mit 3 `AddColumn`-Calls (IsCancelled `bit` defaultValue `false`, CancelledAt `datetime2` nullable, CancelledBy `nvarchar(256)` nullable) und einem `CreateIndex` auf `IsCancelled`.

> Notiere die generierte `<timestamp>_AddProductionOrderCancellation`-MigrationId (Format `20260707xxxxxx_...`) — sie wird in Step 3 und 4 gebraucht.

- [ ] **Step 2: PendingModelChanges leer**

Run:
```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet ef migrations has-pending-model-changes --project IdealAkeWms
```
Expected: `No changes have been made to the model since the last migration were added.` (bzw. "No changes ...").

- [ ] **Step 3: Idempotentes `SQL/80_AddProductionOrderCancellation.sql`**

Ersetze `<MIGRATIONID>` unten mit der in Step 1 notierten MigrationId. Erstelle `SQL/80_AddProductionOrderCancellation.sql`:

```sql
-- ============================================================================
-- Migration 80: AddProductionOrderCancellation (v1.25.0)
-- ============================================================================
-- FA-Reconciliation: verwaiste FAs, die in Sage geloescht wurden, werden vom
-- Service-Sync auf IsCancelled=1 gesetzt (verschwinden aus offenen Sichten).
-- Additiv, Default IsCancelled=0 -> Alt-FAs bleiben unveraendert offen. Idempotent.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.ProductionOrders', 'IsCancelled') IS NULL
    BEGIN
        ALTER TABLE [dbo].[ProductionOrders]
            ADD [IsCancelled] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_IsCancelled] DEFAULT 0;
        PRINT 'Spalte ProductionOrders.IsCancelled angelegt.';
    END

    IF COL_LENGTH('dbo.ProductionOrders', 'CancelledAt') IS NULL
    BEGIN
        ALTER TABLE [dbo].[ProductionOrders] ADD [CancelledAt] DATETIME2 NULL;
        PRINT 'Spalte ProductionOrders.CancelledAt angelegt.';
    END

    IF COL_LENGTH('dbo.ProductionOrders', 'CancelledBy') IS NULL
    BEGIN
        ALTER TABLE [dbo].[ProductionOrders] ADD [CancelledBy] NVARCHAR(256) NULL;
        PRINT 'Spalte ProductionOrders.CancelledBy angelegt.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionOrders_IsCancelled' AND object_id = OBJECT_ID('dbo.ProductionOrders'))
    BEGIN
        CREATE INDEX [IX_ProductionOrders_IsCancelled] ON [dbo].[ProductionOrders] ([IsCancelled]);
        PRINT 'Index IX_ProductionOrders_IsCancelled angelegt.';
    END

    IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '<MIGRATIONID>')
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('<MIGRATIONID>', '10.0.2');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
```

> Der Index-Name `IX_ProductionOrders_IsCancelled` MUSS mit dem EF-generierten Namen übereinstimmen. Öffne die generierte Migration und prüfe den `name:`-Parameter des `CreateIndex`-Aufrufs — falls EF einen anderen Namen wählt, übernimm exakt diesen in das SQL-Skript.

- [ ] **Step 4: FreshInstall — Schema-Block**

In `SQL/00_FreshInstall.sql` im ProductionOrders-CREATE (Block ab Zeile 220). Ersetze die Zeile 232:

```sql
        [IsDone]                  BIT               NOT NULL DEFAULT 0,
```

durch:

```sql
        [IsDone]                  BIT               NOT NULL DEFAULT 0,
        [IsCancelled]             BIT               NOT NULL DEFAULT 0,
        [CancelledAt]             DATETIME2         NULL,
        [CancelledBy]             NVARCHAR(256)     NULL,
```

- [ ] **Step 5: FreshInstall — Index**

In `SQL/00_FreshInstall.sql` sind die ProductionOrders-Indexe als separate `IF NOT EXISTS … CREATE INDEX`-Blöcke gebündelt (Zeilen 1030-1041). Der `IX_ProductionOrders_IsDone`-Block steht bei Zeile 1034-1035:

```sql
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_IsDone')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_IsDone] ON [dbo].[ProductionOrders]([IsDone]);
```

Direkt danach (nach Zeile 1035) einen analogen Block für `IsCancelled` einfügen — im gleichen Stil:

```sql
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionOrders_IsCancelled')
    CREATE NONCLUSTERED INDEX [IX_ProductionOrders_IsCancelled] ON [dbo].[ProductionOrders]([IsCancelled]);
```

> Der Index-Name `IX_ProductionOrders_IsCancelled` MUSS mit dem in `SQL/80` (Step 3) und dem EF-generierten Namen übereinstimmen — prüfe den `name:`-Parameter des `CreateIndex`-Aufrufs in der generierten Migration und gleiche alle drei Stellen ab.

- [ ] **Step 6: FreshInstall — History-Insert**

In `SQL/00_FreshInstall.sql` NACH dem letzten History-Insert-Block (aktuell Zeile 2080-2081, `20260707113155_AddStockReadRole`) anfügen — `<MIGRATIONID>` ersetzen:

```sql

IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '<MIGRATIONID>')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('<MIGRATIONID>', '10.0.2');
```

> **Achtung:** Feature 1 (Hauptlagerplatz) belegt evtl. `SQL/79` + eine eigene Migration mit einem früheren/späteren Timestamp. Prüfe vor dem History-Insert die Reihenfolge: History-Inserts in FreshInstall sind chronologisch nach MigrationId sortiert. Setze den FA-Reconciliation-Insert an die chronologisch korrekte Stelle (nach allen bestehenden Inserts, sofern dieser Timestamp der jüngste ist).

- [ ] **Step 7: Build (App startet Migration nicht — nur kompilieren)**

Run: `dotnet build IdealAkeWms.slnx`
Expected: `Build succeeded.` `0 Error(s)`.

- [ ] **Step 8: Commit**

Run:
```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Migrations SQL/80_AddProductionOrderCancellation.sql SQL/00_FreshInstall.sql && git commit -m "$(cat <<'EOF'
feat(db): Migration 80 AddProductionOrderCancellation + SQL/80 + FreshInstall (v1.25.0)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Reiner Helper `ProductionOrderReconciler` (TDD)

**Files:**
- Create: `IDEALAKEWMSService/Services/ProductionOrderReconciler.cs`
- Create (Test): `IDEALAKEWMSService.Tests/Services/ProductionOrderReconcilerTests.cs`

- [ ] **Step 1: Failing Tests schreiben**

Erstelle `IDEALAKEWMSService.Tests/Services/ProductionOrderReconcilerTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class ProductionOrderReconcilerTests
{
    private static WmsOrderState Wms(string orderNumber, bool isDone = false, bool isCancelled = false)
        => new(orderNumber, isDone, isCancelled);

    [Fact]
    public void Plan_EmptySageRead_SkipsWithGuard_NothingToCancelOrReactivate()
    {
        var sage = new List<string>(); // leer -> Guard
        var wms = new[] { Wms("FA-1"), Wms("FA-CANCELLED", isCancelled: true) };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 100);

        plan.Skipped.Should().BeTrue();
        plan.SkipReason.Should().Be("Sage-Read leer");
        plan.ToCancel.Should().BeEmpty();
        plan.ToReactivate.Should().BeEmpty();
    }

    [Fact]
    public void Plan_OrphanOpenFa_GoesToCancel()
    {
        var sage = new[] { "FA-1" };
        var wms = new[] { Wms("FA-1"), Wms("FA-ORPHAN") };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 100);

        plan.Skipped.Should().BeFalse();
        plan.ToCancel.Should().ContainSingle().Which.Should().Be("FA-ORPHAN");
        plan.ToReactivate.Should().BeEmpty();
    }

    [Fact]
    public void Plan_DoneFaNotInSage_IsNotCancelled()
    {
        // Erledigte FAs sind nicht "offen" -> werden nie storniert.
        var sage = new[] { "FA-1" };
        var wms = new[] { Wms("FA-1"), Wms("FA-DONE", isDone: true) };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 100);

        plan.ToCancel.Should().BeEmpty();
    }

    [Fact]
    public void Plan_CancelledFaBackInSage_GoesToReactivate()
    {
        var sage = new[] { "FA-1", "FA-BACK" };
        var wms = new[] { Wms("FA-1"), Wms("FA-BACK", isCancelled: true) };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 100);

        plan.ToReactivate.Should().ContainSingle().Which.Should().Be("FA-BACK");
        plan.ToCancel.Should().BeEmpty();
    }

    [Fact]
    public void Plan_CancelledFaStillGone_StaysCancelled_NoDoubleCancel()
    {
        var sage = new[] { "FA-1" };
        var wms = new[] { Wms("FA-1"), Wms("FA-STILLGONE", isCancelled: true) };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 100);

        plan.ToCancel.Should().BeEmpty();       // bereits storniert -> nicht erneut
        plan.ToReactivate.Should().BeEmpty();   // nicht in Sage -> nicht reaktivieren
        plan.Skipped.Should().BeFalse();
    }

    [Fact]
    public void Plan_ToCancelExceedsCap_Skips_ButReactivationsSurvive()
    {
        var sage = new[] { "FA-KEEP", "FA-BACK" };
        var wms = new[]
        {
            Wms("FA-KEEP"),
            Wms("FA-BACK", isCancelled: true), // Reaktivierung
            Wms("FA-ORPHAN-A"),
            Wms("FA-ORPHAN-B"),
            Wms("FA-ORPHAN-C"),
        };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 2);

        plan.Skipped.Should().BeTrue();
        plan.SkipReason.Should().Be("Cap ueberschritten (3 > 2)");
        plan.ToCancel.Should().BeEmpty(); // geleert
        plan.ToReactivate.Should().BeEquivalentTo(new[] { "FA-BACK" }); // Reaktivierungen bleiben
    }

    [Fact]
    public void Plan_OrderNumberMatching_IsCaseInsensitiveAndTrimmed()
    {
        // Defensive: Sage-View kann Trailing-Spaces liefern; Casing egal.
        var sage = new[] { " fa-1 " };
        var wms = new[] { Wms("FA-1") };

        var plan = ProductionOrderReconciler.Plan(sage, wms, maxCancelPerRun: 100);

        plan.ToCancel.Should().BeEmpty(); // FA-1 gilt als in Sage vorhanden
    }
}
```

- [ ] **Step 2: Test läuft rot (Helper existiert noch nicht)**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo`
Expected: **Compile-Fehler** (`ProductionOrderReconciler`/`WmsOrderState` unbekannt) — das ist der rote Zustand. (TDD: Test compiliert erst nach Step 3.)

- [ ] **Step 3: Helper implementieren**

Erstelle `IDEALAKEWMSService/Services/ProductionOrderReconciler.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Zustand einer WMS-FA fuer den Reconcile-Abgleich (rein wertbasiert, keine DB).
/// </summary>
public sealed record WmsOrderState(string OrderNumber, bool IsDone, bool IsCancelled);

/// <summary>
/// Ergebnis des Reconcile-Abgleichs. <see cref="ToCancel"/>/<see cref="ToReactivate"/>
/// sind OrderNumbers. Bei <see cref="Skipped"/>=true wird KEIN Storno geschrieben
/// (Reaktivierungen bleiben trotzdem gueltig — siehe Cap-Regel).
/// </summary>
public sealed record ReconcilePlan(
    IReadOnlyList<string> ToCancel,
    IReadOnlyList<string> ToReactivate,
    bool Skipped,
    string? SkipReason);

/// <summary>
/// Reine Reconcile-Logik (voll unit-testbar, keine DB). Vergleicht die aus der
/// Sage-View gelesenen OrderNumbers mit dem WMS-Zustand und liefert, was storniert
/// bzw. reaktiviert werden muss. Guard (leerer Sage-Read) + Cap schuetzen vor
/// versehentlichem Massen-Stornieren bei Sage-Teil-/Ausfaellen.
/// </summary>
public static class ProductionOrderReconciler
{
    public static ReconcilePlan Plan(
        IReadOnlyCollection<string> sageOrderNumbers,
        IReadOnlyCollection<WmsOrderState> wmsOrders,
        int maxCancelPerRun)
    {
        // Guard: leerer Sage-Read -> nichts anfassen (Sage-Ausfall/Teil-Read).
        if (sageOrderNumbers.Count == 0)
        {
            return new ReconcilePlan(
                Array.Empty<string>(), Array.Empty<string>(),
                Skipped: true, SkipReason: "Sage-Read leer");
        }

        var sageSet = new HashSet<string>(
            sageOrderNumbers
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim()),
            StringComparer.OrdinalIgnoreCase);

        // Reaktivieren: WMS-storniert, aber wieder in Sage vorhanden.
        var toReactivate = wmsOrders
            .Where(o => o.IsCancelled && sageSet.Contains(o.OrderNumber.Trim()))
            .Select(o => o.OrderNumber)
            .ToList();

        // Stornieren: offen (nicht IsDone, nicht bereits storniert) UND nicht in Sage.
        var toCancel = wmsOrders
            .Where(o => !o.IsDone && !o.IsCancelled && !sageSet.Contains(o.OrderNumber.Trim()))
            .Select(o => o.OrderNumber)
            .ToList();

        // Cap: zu viele Storni -> kein Storno (aber Reaktivierungen bleiben).
        if (toCancel.Count > maxCancelPerRun)
        {
            return new ReconcilePlan(
                Array.Empty<string>(), toReactivate,
                Skipped: true,
                SkipReason: $"Cap ueberschritten ({toCancel.Count} > {maxCancelPerRun})");
        }

        return new ReconcilePlan(toCancel, toReactivate, Skipped: false, SkipReason: null);
    }
}
```

> `using System;` wird über die ImplicitUsings des Service-Projekts (`<ImplicitUsings>enable</ImplicitUsings>`) bereitgestellt — `Array.Empty<string>()` und `StringComparer` sind ohne expliziten `using System;` verfügbar. Falls der Build ein `System`-Symbol vermisst, `using System;` oben ergänzen.

- [ ] **Step 4: Tests grün**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo`
Expected: `Passed!` — alle 7 neuen `ProductionOrderReconcilerTests` grün, keine Regression.

- [ ] **Step 5: Commit**

Run:
```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IDEALAKEWMSService/Services/ProductionOrderReconciler.cs IDEALAKEWMSService.Tests/Services/ProductionOrderReconcilerTests.cs && git commit -m "$(cat <<'EOF'
feat(service): reiner ProductionOrderReconciler-Helper + Unit-Tests (v1.25.0)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: EF-Offen-Queries um `!IsCancelled` erweitern (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs:5-17` (`LeitstandOrderRow`)
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs:42,64-76,84,118-121`
- Modify: `IdealAkeWms/Controllers/FaCompletionController.cs:94`
- Modify: `IdealAkeWms/Controllers/FaWorklistController.cs:150-153`
- Modify: `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs:35` (Badge-Feld — für Task 8 vorbereitet)
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs:116` (IsCancelled ins ViewItem)
- Modify (Test): `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs`

- [ ] **Step 1: Failing Tests schreiben**

Ans Ende von `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs` (vor der schließenden Klasse-`}`) einfügen:

```csharp
    [Fact]
    public async Task GetOpenOrdersAsync_ExcludesCancelledOrders()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionOrders.Add(new ProductionOrder { OrderNumber = "FA-OPEN", IsDone = false, IsCancelled = false });
        ctx.ProductionOrders.Add(new ProductionOrder { OrderNumber = "FA-CANCELLED", IsDone = false, IsCancelled = true });
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderRepository(ctx);
        var result = await repo.GetOpenOrdersAsync();

        result.Should().ContainSingle().Which.OrderNumber.Should().Be("FA-OPEN");
    }

    [Fact]
    public async Task GetForLeitstand_ExcludesCancelled_WhenShowDoneFalse()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);

        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-OPEN", IsDone = false, IsCancelled = false });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-CANCELLED", IsDone = false, IsCancelled = true });
        await ctx.SaveChangesAsync();

        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: false, page: 1, pageSize: 100);

        page.Rows.Should().ContainSingle(r => r.OrderNumber == "FA-OPEN");
        page.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetForLeitstand_IncludesCancelledWithFlag_WhenShowDoneTrue()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);

        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-OPEN", IsDone = false, IsCancelled = false });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-CANCELLED", IsDone = false, IsCancelled = true });
        await ctx.SaveChangesAsync();

        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: true, page: 1, pageSize: 100);

        page.Rows.Should().HaveCount(2);
        page.Rows.Single(r => r.OrderNumber == "FA-CANCELLED").IsCancelled.Should().BeTrue();
        page.Rows.Single(r => r.OrderNumber == "FA-OPEN").IsCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOpenOrdersInWindow_ExcludesCancelled()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);

        var inWindow = DateTime.Now.AddDays(7);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-OPEN", IsDone = false, IsCancelled = false, ProductionDate = inWindow });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-CANCELLED", IsDone = false, IsCancelled = true, ProductionDate = inWindow });
        await ctx.SaveChangesAsync();

        var result = await repo.GetOpenOrdersInWindowAsync(weeksAhead: 8, maxCount: 200);

        result.Should().ContainSingle().Which.OrderNumber.Should().Be("FA-OPEN");
    }
```

- [ ] **Step 2: Test läuft rot**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ProductionOrderRepositoryTests"`
Expected: Compile-Fehler (`LeitstandOrderRow` hat kein `IsCancelled`) ODER — nach dem Row-Fix — assertion-fails, weil die Filter noch nicht greifen. Beides ist „rot". (Wenn nur assertion-fails: 2 der 4 neuen Tests scheitern, weil FA-CANCELLED noch mitkommt.)

- [ ] **Step 3: `LeitstandOrderRow` um `IsCancelled` erweitern**

In `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs` das Record (Zeile 5-17) ändern — NACH `bool IsDonePicking,` (Zeile 16) das Feld ergänzen. Neuer Record:

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
    bool IsCancelled,
    string? WorkplaceName);
```

- [ ] **Step 4: Repository-Filter + Projection**

In `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs`:

(a) `GetForLeitstandAsync` — Zeile 41-42 (`if (!showDone)`-Block) ändern zu:

```csharp
        if (!showDone)
            q = q.Where(o => !o.IsDone && !o.IsCancelled && (o.PickingStatus == null || !o.PickingStatus.IsDonePicking));
```

(b) `GetForLeitstandAsync` — die `.Select(...)`-Projection (Zeile 64-76). NACH `o.PickingStatus != null && o.PickingStatus.IsDonePicking,` (Zeile 75) und VOR der Workplace-Zeile (Zeile 76) das neue Feld einfügen, sodass die Argument-Reihenfolge exakt dem erweiterten Record entspricht:

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
                o.IsCancelled,
                o.ProductionWorkplace != null ? o.ProductionWorkplace.Name : null))
```

(c) `GetOpenOrdersAsync` — Zeile 84 ändern zu:

```csharp
        return await _dbSet.Where(o => !o.IsDone && !o.IsCancelled).OrderBy(o => o.OrderNumber).ToListAsync();
```

(d) `GetOpenOrdersInWindowAsync` — die `.Where(...)` (Zeile 118-121) um `&& !po.IsCancelled` ergänzen:

```csharp
            .Where(po => !po.IsDone
                         && !po.IsCancelled
                         && !(po.PickingStatus != null && po.PickingStatus.IsDonePicking)
                         && po.ProductionDate != null
                         && po.ProductionDate <= cutoff)
```

- [ ] **Step 5: FaCompletion + FaWorklist Offen-Filter**

(a) `IdealAkeWms/Controllers/FaCompletionController.cs` — Zeile 93-95 (im `if (!showDone)`-Block) ändern zu:

```csharp
            orders = orders
                .Where(o => !o.IsDone && !o.IsCancelled && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking))
                .ToList();
```

(b) `IdealAkeWms/Controllers/FaWorklistController.cs` — der `orders`-Filter Zeile 150-153. `&& !o.IsCancelled` ergänzen:

```csharp
        var orders = (await _productionOrderRepository.GetAllOrderedAsync())
            .Where(o => !o.IsDone
                        && !o.IsCancelled
                        && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking)
                        && orderIdsWithStep.Contains(o.Id)
                        && WorkbenchFilter.Matches(o.ProductionWorkplace?.Name, effectiveWorkbenches))
            .ToList();
```

> `GetAllOrderedAsync` lädt bereits `ProductionOrder`-Entities (mit dem neuen `IsCancelled`-Feld) — kein zusätzliches `.Include` nötig, `o.IsCancelled` ist direkt am Entity.

- [ ] **Step 6: ViewModel + ProductionOrdersController (Badge-Vorbereitung)**

(a) `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs` — an `ProductionOrderListItem` NACH `public bool IsDone { get; set; }` (Zeile 35) ergänzen:

```csharp
    public bool IsCancelled { get; set; }
```

(b) `IdealAkeWms/Controllers/ProductionOrdersController.cs` — im ViewItem-Mapping NACH `IsDone = o.IsDone || o.IsDonePicking,` (Zeile 116) ergänzen:

```csharp
                IsCancelled = o.IsCancelled,
```

- [ ] **Step 7: Tests grün**

Run: `dotnet test IdealAkeWms.Tests --nologo`
Expected: `Passed!` — die 4 neuen Repo-Tests grün, keine Regression. (Die bestehenden `GetForLeitstand_*`-Tests bleiben grün, da FA-CANCELLED dort nicht vorkommt.)

- [ ] **Step 8: Commit**

Run:
```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs IdealAkeWms/Controllers/FaCompletionController.cs IdealAkeWms/Controllers/FaWorklistController.cs IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs IdealAkeWms/Controllers/ProductionOrdersController.cs IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs && git commit -m "$(cat <<'EOF'
feat(web): IsCancelled in allen EF-Offen-Queries (Repo/FaCompletion/FaWorklist) + Tests (v1.25.0)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Service-Kandidaten-Queries + BomCache-raw-SQL um `IsCancelled` erweitern

**Files:**
- Modify: `IDEALAKEWMSService/Services/FaWorkStepDetectionService.cs:88-96` (beide Kandidaten-Queries — EF/LINQ)
- Modify: `IDEALAKEWMSService/Services/BomCacheSyncService.cs:396-404` (raw-SQL `whereClause`)
- Modify (Test): `IDEALAKEWMSService.Tests/Services/FaWorkStepDetectionServiceTests.cs`

> **Hinweis zur Testbarkeit:** `FaWorkStepDetectionService` nutzt EF Core gegen den geteilten `ApplicationDbContext` — es IST also InMemory-testbar (anders als die Spec/CLAUDE.md-Formulierung „raw-SQL" nahelegt; die Spec meint hier den Detection-Schritt insgesamt). Deshalb bekommt es einen echten InMemory-Test. Der **BomCache**-`whereClause` ist echte raw-SQL (`SqlConnection`) → NICHT InMemory-testbar → nur Build + Manual-UAT.

- [ ] **Step 1: Failing Test für Detection schreiben**

An `IDEALAKEWMSService.Tests/Services/FaWorkStepDetectionServiceTests.cs` (vor der schließenden Klasse-`}`) einen Test anhängen. Der `NewOrder`-Helper (Zeile 25-31) unterstützt `isCancelled` noch nicht — daher inline-Objekt bauen:

```csharp
    [Fact]
    public async Task Detect_SkipsCancelledOrders()
    {
        using var ctx = TestDbContextFactory.Create();
        var step = NewWorkStep("VL", "luefter");
        // Offene FA -> wird erkannt.
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            OrderNumber = "FA-OPEN", ArticleNumber = "ART-1", IsDone = false, IsCancelled = false,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
        });
        // Stornierte FA (gleiche Artikelnummer) -> darf NICHT erkannt werden.
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            OrderNumber = "FA-CANCELLED", ArticleNumber = "ART-1", IsDone = false, IsCancelled = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
        });
        ctx.WorkSteps.Add(step);
        ctx.CachedBomHeaders.Add(NewBomHeader("ART-1", "Axialluefter 230V"));
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).DetectAsync(dryRun: false);

        result.Inserted.Should().Be(1);
        var row = await ctx.FaWorkSteps.SingleAsync();
        var openOrder = await ctx.ProductionOrders.SingleAsync(o => o.OrderNumber == "FA-OPEN");
        row.ProductionOrderId.Should().Be(openOrder.Id);
    }
```

- [ ] **Step 2: Test rot**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~FaWorkStepDetectionServiceTests"`
Expected: `Detect_SkipsCancelledOrders` schlägt fehl — `result.Inserted` ist 2 statt 1 (die stornierte FA wird noch mit-erkannt), oder `ctx.FaWorkSteps` hat 2 Zeilen.

- [ ] **Step 3: Detection-Queries fixen**

In `IDEALAKEWMSService/Services/FaWorkStepDetectionService.cs` beide Kandidaten-Queries (Zeile 88-99) um `&& !o.IsCancelled` ergänzen:

```csharp
                var matchedFaCount = await _db.ProductionOrders
                    .Where(o => !o.IsDone
                             && !o.IsCancelled
                             && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking)
                             && o.ArticleNumber != null && stepMatchedArticles.Contains(o.ArticleNumber!))
                    .CountAsync(ct);
                var candidates = await _db.ProductionOrders
                    .Where(o => !o.IsDone
                             && !o.IsCancelled
                             && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking)
                             && o.ArticleNumber != null && stepMatchedArticles.Contains(o.ArticleNumber!))
                    .Where(o => !_db.FaWorkSteps.Any(f => f.ProductionOrderId == o.Id && f.WorkStepId == step.Id))
                    .Select(o => new { o.Id, o.OrderNumber, o.ArticleNumber })
                    .ToListAsync(ct);
```

- [ ] **Step 4: BomCache raw-SQL fixen**

In `IDEALAKEWMSService/Services/BomCacheSyncService.cs` den `whereClause`-Konstanten (Zeile 396-404) um `AND po.[IsCancelled] = 0` ergänzen. Neuer Block:

```csharp
        // "Abgeschlossen" = IsDone (Sage) ODER IsDonePicking (App). Komm-abgeschlossene FAs
        // (IsDone=0, IsDonePicking=1) duerfen das Fenster NICHT belegen (Web-Semantik v1.21.1).
        // Stornierte FAs (IsCancelled=1, FA-Reconciliation v1.25.0) ebenfalls ausschliessen.
        const string whereClause = @"
            WHERE po.[IsDone] = 0
              AND po.[IsCancelled] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[ProductionOrderPickingStatus] ps
                  WHERE ps.[ProductionOrderId] = po.[Id] AND ps.[IsDonePicking] = 1
              )
              AND po.[ProductionDate] IS NOT NULL
              AND po.[ProductionDate] <= DATEADD(week, @weeks, GETDATE())
              AND po.[ArticleNumber] IS NOT NULL";
```

- [ ] **Step 5: Detection-Test grün + Build**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~FaWorkStepDetectionServiceTests"`
Expected: `Passed!` — inkl. `Detect_SkipsCancelledOrders`.

Run: `dotnet build IdealAkeWms.slnx`
Expected: `Build succeeded.` `0 Error(s)`. (BomCache-raw-SQL nur kompiliert — Verhalten ist Manual-UAT.)

- [ ] **Step 6: Commit**

Run:
```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IDEALAKEWMSService/Services/FaWorkStepDetectionService.cs IDEALAKEWMSService/Services/BomCacheSyncService.cs IDEALAKEWMSService.Tests/Services/FaWorkStepDetectionServiceTests.cs && git commit -m "$(cat <<'EOF'
feat(service): IsCancelled aus Detection- + BomCache-Offen-Queries ausschliessen (v1.25.0)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Reconcile in `SageImportService.SyncProductionOrdersAsync` verdrahten

**Files:**
- Modify: `IDEALAKEWMSService/Services/SageImportService.cs:13-28` (Konstruktor: `ISyncErrorNotifier` injizieren), `:96-210` (Reconcile nach Upsert)
- Modify (Test): `IDEALAKEWMSService.Tests/Services/SageImportServiceTests.cs:12-23` (Build-Helper um Notifier-Mock ergänzen)

> **Testbarkeit:** Die Verdrahtung läuft über raw ADO.NET (`SqlConnection` gegen WMS + Sage) → NICHT InMemory-testbar. Die Entscheidungslogik ist bereits in `ProductionOrderReconciler` (Task 3) unit-getestet. Hier: nur Kompilierbarkeit + bestehende Lifecycle-Tests grün halten; Verhalten ist **Manual-UAT** (Task 9 TESTSZENARIEN).

- [ ] **Step 1: Test-Build-Helper anpassen (Konstruktor bekommt Notifier)**

`SageImportServiceTests` baut den Service mit 5 Argumenten (Zeile 17-22). Nach der Konstruktor-Erweiterung braucht es ein 6. Argument. In `IDEALAKEWMSService.Tests/Services/SageImportServiceTests.cs` die `Build`-Methode (Zeile 12-23) ändern zu:

```csharp
    private static SageImportService Build(FakeSyncLogger fakeLogger)
    {
        var config = new ConfigurationBuilder().Build(); // no connection strings
        var bomMock = new Mock<IBomCacheSyncService>();
        var coatingMock = new Mock<ICoatingDetectionService>();
        var notifierMock = new Mock<ISyncErrorNotifier>();
        return new SageImportService(
            config,
            NullLogger<SageImportService>.Instance,
            fakeLogger,
            bomMock.Object,
            coatingMock.Object,
            notifierMock.Object);
    }
```

> Die beiden bestehenden Tests (`SyncProductionOrdersAsync_writes_lifecycle_via_failure_path`, `SyncArticlesAsync_...`) scheitern jetzt am Compile (falscher Konstruktor) — das ist erwartet, sie werden nach Step 2 grün. Das ist der „rote" TDD-Zustand für diesen Task.

- [ ] **Step 2: Konstruktor + Feld in `SageImportService`**

In `IDEALAKEWMSService/Services/SageImportService.cs` das Feld nach Zeile 14 (`private readonly ICoatingDetectionService _coatingDetection;`) ergänzen:

```csharp
    private readonly ISyncErrorNotifier _errorNotifier;
```

Und den Konstruktor (Zeile 16-28) erweitern — `errorNotifier` als letzten Parameter:

```csharp
    public SageImportService(
        IConfiguration configuration,
        ILogger<SageImportService> logger,
        ISyncLogger syncLogger,
        IBomCacheSyncService bomCacheSync,
        ICoatingDetectionService coatingDetection,
        ISyncErrorNotifier errorNotifier)
    {
        _configuration = configuration;
        _logger = logger;
        _syncLogger = syncLogger;
        _bomCacheSync = bomCacheSync;
        _coatingDetection = coatingDetection;
        _errorNotifier = errorNotifier;
    }
```

> DI: `ISyncErrorNotifier` + `ISageImportService` sind beide `AddScoped` in `Program.cs` (Zeile 51 + 64) — die Auflösung funktioniert ohne weitere Registrierung.

- [ ] **Step 3: Sage-OrderNumber-Set beim Lesen sammeln**

Der Reconcile braucht das Set aller Sage-OrderNumbers. `sageOrders` (Zeile 58-81) enthält sie bereits — kein zweiter Read nötig. Das Set wird in Step 5 direkt aus `sageOrders.Select(o => o.OrderNumber)` gebildet.

- [ ] **Step 4: Reconcile-Config lesen**

In `SyncProductionOrdersAsync` NACH dem BOM-Cache/Coating-Hook-Block (nach Zeile 200, vor der `_logger.LogInformation("Produktionsaufträge-Sync abgeschlossen...")`-Zeile 202) einen neuen Block einfügen, der den Reconcile ausführt. Vollständiger Block:

```csharp
            // ---- FA-Reconciliation (v1.25.0) ----------------------------------
            // Verwaiste WMS-offene FAs, die nicht mehr in der Sage-View sind, stornieren;
            // wieder aufgetauchte reaktivieren. Guard (leerer Read) + Cap schuetzen vor
            // versehentlichem Massen-Stornieren. Opt-in per Flag; DryRun schreibt nichts.
            var reconcileEnabled = await ServiceSettings.GetBoolAsync(
                _configuration, "Sync:ProductionOrderReconcileEnabled", false, ct);
            var maxCancelPerRun = await ServiceSettings.GetIntAsync(
                _configuration, "Sync:ReconcileMaxCancelPerRun", 100, ct);

            var sageOrderNumbers = sageOrders.Select(o => o.OrderNumber).ToList();

            // WMS-Zustaende laden: alle offenen ODER stornierten FAs.
            var wmsStates = new List<WmsOrderState>();
            await using (var stateCmd = new SqlCommand(
                "SELECT [OrderNumber], [IsDone], [IsCancelled] FROM [dbo].[ProductionOrders] WHERE [IsDone] = 0 OR [IsCancelled] = 1",
                wmsConn) { CommandTimeout = 120 })
            await using (var stateReader = await stateCmd.ExecuteReaderAsync(ct))
            {
                while (await stateReader.ReadAsync(ct))
                {
                    wmsStates.Add(new WmsOrderState(
                        stateReader.GetString(0),
                        stateReader.GetBoolean(1),
                        stateReader.GetBoolean(2)));
                }
            }

            var reconcilePlan = ProductionOrderReconciler.Plan(sageOrderNumbers, wmsStates, maxCancelPerRun);
            int cancelled = 0, reactivated = 0;

            if (!reconcileEnabled)
            {
                _logger.LogInformation(
                    "FA-Reconciliation deaktiviert (Sync:ProductionOrderReconcileEnabled=false) — Plan: {Cancel} Storno-Kandidaten, {React} Reaktivierungen (nichts geschrieben).",
                    reconcilePlan.ToCancel.Count, reconcilePlan.ToReactivate.Count);
            }
            else if (dryRun)
            {
                _logger.LogInformation(
                    "[DryRun] FA-Reconciliation — {Cancel} Storno-Kandidaten, {React} Reaktivierungen (nichts geschrieben). Skipped={Skipped} {Reason}",
                    reconcilePlan.ToCancel.Count, reconcilePlan.ToReactivate.Count,
                    reconcilePlan.Skipped, reconcilePlan.SkipReason);
                await run.LogInfoAsync(
                    $"[DryRun] Reconcile: {reconcilePlan.ToCancel.Count} wuerden storniert, {reconcilePlan.ToReactivate.Count} reaktiviert.", ct: ct);
            }
            else
            {
                // Reaktivieren laeuft IMMER (auch bei Guard/Cap-Skip — Reaktivierungen sind nie gefaehrlich).
                foreach (var orderNumber in reconcilePlan.ToReactivate)
                {
                    await using var reactCmd = new SqlCommand(
                        "UPDATE [dbo].[ProductionOrders] SET [IsCancelled] = 0, [CancelledAt] = NULL, [CancelledBy] = NULL, " +
                        "[ModifiedAt] = GETUTCDATE(), [ModifiedBy] = 'IDEALAKEWMSService', [ModifiedByWindows] = SYSTEM_USER " +
                        "WHERE [OrderNumber] = @OrderNumber",
                        wmsConn) { CommandTimeout = 60 };
                    reactCmd.Parameters.AddWithValue("@OrderNumber", orderNumber);
                    reactivated += await reactCmd.ExecuteNonQueryAsync(ct);
                }

                if (reconcilePlan.Skipped)
                {
                    var reason = reconcilePlan.SkipReason ?? "unbekannt";
                    _logger.LogWarning("FA-Reconciliation uebersprungen: {Reason}. Kein Storno geschrieben.", reason);
                    await run.LogWarningAsync($"Reconcile uebersprungen: {reason}. Kein Storno.", ct: ct);

                    // Cap-Skip zusaetzlich per Fehlermail melden (Sage-Teil-Read-Verdacht).
                    if (reason.StartsWith("Cap", StringComparison.OrdinalIgnoreCase))
                    {
                        await _errorNotifier.NotifyAsync(
                            SyncLogServices.ProductionOrder,
                            new InvalidOperationException(
                                $"FA-Reconciliation Cap ueberschritten: {reason}. Sage lieferte {sageOrderNumbers.Count} FAs, " +
                                $"Cap={maxCancelPerRun}. Kein Storno geschrieben — moeglicher Sage-Teil-Read."),
                            ct);
                    }
                }
                else
                {
                    var now = DateTime.Now;
                    foreach (var orderNumber in reconcilePlan.ToCancel)
                    {
                        await using var cancelCmd = new SqlCommand(
                            "UPDATE [dbo].[ProductionOrders] SET [IsCancelled] = 1, [CancelledAt] = @Now, [CancelledBy] = 'System-Reconcile', " +
                            "[ModifiedAt] = GETUTCDATE(), [ModifiedBy] = 'IDEALAKEWMSService', [ModifiedByWindows] = SYSTEM_USER " +
                            "WHERE [OrderNumber] = @OrderNumber AND [IsDone] = 0 AND [IsCancelled] = 0",
                            wmsConn) { CommandTimeout = 60 };
                        cancelCmd.Parameters.AddWithValue("@Now", now);
                        cancelCmd.Parameters.AddWithValue("@OrderNumber", orderNumber);
                        cancelled += await cancelCmd.ExecuteNonQueryAsync(ct);
                    }
                }

                _logger.LogInformation(
                    "FA-Reconciliation: {Cancelled} storniert, {Reactivated} reaktiviert.", cancelled, reactivated);
            }
            // ---- Ende FA-Reconciliation ---------------------------------------
```

- [ ] **Step 5: Counts-Keys ergänzen**

Der `FinishSuccessAsync`-Aufruf (Zeile 204-208) bekommt die zwei neuen Keys. Ändern zu:

```csharp
            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["storniert"] = cancelled,
                ["reaktiviert"] = reactivated,
            }, ct: ct);
```

> Der frühe DryRun-`return` (Zeile 85-94) bleibt unverändert — im DryRun-Zweig wird der reguläre Upsert übersprungen und daher auch der Reconcile-Block nicht erreicht. **Wichtig:** Der Reconcile läuft damit NUR im Nicht-DryRun-Pfad. Für eine echte DryRun-Reconcile-Vorschau muss der Admin das Flag `Sync:ProductionOrderReconcileEnabled` auf `false` lassen (dann läuft der reguläre Upsert, der Reconcile-Block loggt aber nur den Plan ohne zu schreiben — siehe `!reconcileEnabled`-Zweig). Diese Semantik in TESTSZENARIEN (Task 9) dokumentieren: „DryRun-Kontroll-Lauf" = Flag AUS bei aktivem (nicht-DryRun) Sync.

- [ ] **Step 6: `using`-Check**

`SageImportService.cs` hat oben bereits `using IdealAkeWms.Services.SyncLogger;`, `using IDEALAKEWMSService.Common;`, `using Microsoft.Data.SqlClient;`. `WmsOrderState`/`ProductionOrderReconciler`/`ISyncErrorNotifier` liegen im Namespace `IDEALAKEWMSService.Services` (= der Namespace dieser Datei) → kein zusätzlicher `using` nötig. `ServiceSettings` liegt in `IDEALAKEWMSService.Common` (bereits importiert).

- [ ] **Step 7: Build + bestehende Service-Tests grün**

Run: `dotnet build IdealAkeWms.slnx`
Expected: `Build succeeded.` `0 Error(s)`.

Run: `dotnet test IDEALAKEWMSService.Tests --nologo`
Expected: `Passed!` — die beiden `SageImportServiceTests`-Lifecycle-Tests grün (Konstruktor stimmt wieder), alles andere grün.

- [ ] **Step 8: Commit**

Run:
```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IDEALAKEWMSService/Services/SageImportService.cs IDEALAKEWMSService.Tests/Services/SageImportServiceTests.cs && git commit -m "$(cat <<'EOF'
feat(service): FA-Reconcile in SyncProductionOrdersAsync (Flag/Cap/Guard/Mail, storniert+reaktiviert Counts) (v1.25.0)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: Config — 2 neue `Sync:`-Keys

**Files:**
- Modify: `IDEALAKEWMSService/appsettings.json:21-38` (`Sync`-Block)

- [ ] **Step 1: Keys in appsettings.json**

In `IDEALAKEWMSService/appsettings.json` im `"Sync"`-Block (Zeile 21-38) zwei Keys ergänzen — z. B. direkt nach `"FaWorkStepDetectionEnabled": false,` (Zeile 32):

```json
    "FaWorkStepDetectionEnabled": false,
    "ProductionOrderReconcileEnabled": false,
    "ReconcileMaxCancelPerRun": 100,
```

- [ ] **Step 2: ServiceSettings-Lesepfad prüfen**

Der Service liest Settings via `ServiceSettings.GetBoolAsync`/`GetIntAsync(_configuration, "Sync:...")` — beide Keys werden über die vorhandene Config-Bindung (appsettings.json ODER ServiceSettings-DB-Tabelle) aufgelöst. **Kein Code-Change nötig** außer der Nutzung in Task 6 (bereits erledigt). Verifiziere per grep, dass die Keys korrekt referenziert sind:

Run: `grep -rn "ProductionOrderReconcileEnabled\|ReconcileMaxCancelPerRun" IDEALAKEWMSService/`
Expected: Treffer in `appsettings.json` (2×) UND `Services/SageImportService.cs` (2×, aus Task 6).

- [ ] **Step 3: JSON gültig + Build**

Run: `dotnet build IdealAkeWms.slnx`
Expected: `Build succeeded.` (JSON-Syntaxfehler würden den Build nicht brechen, aber der Service-Start; daher zusätzlich validieren:)

Run: `python -c "import json; json.load(open('IDEALAKEWMSService/appsettings.json'))" && echo "JSON OK"`
Expected: `JSON OK`. (Falls kein python: `node -e "require('./IDEALAKEWMSService/appsettings.json'); console.log('JSON OK')"`.)

- [ ] **Step 4: Commit**

Run:
```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IDEALAKEWMSService/appsettings.json && git commit -m "$(cat <<'EOF'
feat(config): Sync:ProductionOrderReconcileEnabled + ReconcileMaxCancelPerRun (Default aus/100) (v1.25.0)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

## Task 8: UI — Badge „In Sage gelöscht" in der FA-Liste

**Files:**
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml:91-93` (order-number-Zelle / Row)

> Datenfluss ist ab Task 4 vorbereitet: `ProductionOrderListItem.IsCancelled` ist gesetzt; `GetForLeitstandAsync` liefert stornierte FAs nur bei `showDone=true` (dann mit `IsCancelled=true`). Der Badge unterscheidet im „Erledigte anzeigen"-Modus zwischen erledigt und storniert.

- [ ] **Step 1: Badge rendern**

In `IdealAkeWms/Views/ProductionOrders/Index.cshtml`. Die OrderNumber-Zelle ist bei Zeile 119-125 (ein `<td>` mit `<strong>@item.OrderNumber</strong>` gefolgt vom `_EnaioDmsBadges`-Partial — die FA-Nummer steckt NICHT in einem Link). Diese Zelle:

```cshtml
                    <td>
                        <strong>@item.OrderNumber</strong>
                        @if (Model.EnaioDmsLinks.TryGetValue(item.OrderNumber, out var dmsLinks))
                        {
                            @await Html.PartialAsync("_EnaioDmsBadges", new IdealAkeWms.Models.ViewModels.EnaioDmsBadgesViewModel(dmsLinks))
                        }
                    </td>
```

ersetzen durch (Badge nach dem `</strong>`, vor den enaio-Badges):

```cshtml
                    <td>
                        <strong>@item.OrderNumber</strong>
                        @if (item.IsCancelled)
                        {
                            <span class="badge bg-danger ms-1" title="Dieser Fertigungsauftrag existiert nicht mehr in Sage und wurde automatisch storniert.">In Sage gelöscht</span>
                        }
                        @if (Model.EnaioDmsLinks.TryGetValue(item.OrderNumber, out var dmsLinks))
                        {
                            @await Html.PartialAsync("_EnaioDmsBadges", new IdealAkeWms.Models.ViewModels.EnaioDmsBadgesViewModel(dmsLinks))
                        }
                    </td>
```

Und die Row-Klasse (Zeile 93) so anpassen, dass stornierte FAs visuell abgesetzt sind (rötlich statt nur grau):

```cshtml
                <tr class="@(item.IsCancelled ? "table-danger" : item.IsDone ? "table-secondary" : "")">
```

- [ ] **Step 2: Build**

Run: `dotnet build IdealAkeWms.slnx`
Expected: `Build succeeded.` `0 Error(s)`. (Razor-Views werden beim Build kompiliert.)

- [ ] **Step 3: Commit**

Run:
```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Views/ProductionOrders/Index.cshtml && git commit -m "$(cat <<'EOF'
feat(ui): Badge 'In Sage geloescht' fuer stornierte FAs in der FA-Liste (v1.25.0)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

## Task 9: Doku — Changelog, CLAUDE.md, TESTSZENARIEN, PROJECT_STATUS

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` (v1.25.0-Card erweitern)
- Modify: `CLAUDE.md` (Fallstrick + ServiceSettings-Tabelle + AppSettings/Rollen ggf.)
- Modify: `docs/TESTSZENARIEN.md` (neues Kapitel + DryRun-Kontroll-Lauf)
- Modify: `PROJECT_STATUS.md`

- [ ] **Step 1: Changelog — v1.25.0-Card erweitern (KEIN neuer Versions-Header)**

In `IdealAkeWms/Views/Help/Changelog.cshtml` die bestehende v1.25.0-Card finden:

Run: `grep -n "1.25.0" IdealAkeWms/Views/Help/Changelog.cshtml`

In die Feature-Liste dieser Card einen `<li>`-Eintrag ergänzen (Stil an die Nachbar-Einträge angleichen):

```html
                <li><strong>FA-Reconciliation:</strong> Der FA-Sync erkennt in Sage gelöschte, bei uns noch offene Fertigungsaufträge und storniert sie automatisch (verschwinden aus allen offenen Sichten; Badge „In Sage gelöscht" bei „Erledigte anzeigen"). Taucht eine FA wieder in Sage auf, wird sie reaktiviert. Sicherheits-Guard (leerer Sage-Read) + Cap (max. Storni/Lauf → Fehlermail) verhindern versehentliches Massen-Stornieren. Opt-in über <code>Sync:ProductionOrderReconcileEnabled</code> (Default aus).</li>
```

- [ ] **Step 2: CLAUDE.md — Fallstrick + Tabellen**

(a) In der **Fallstricke**-Liste (großer Block mit `- **...**:`-Einträgen) einen neuen Eintrag ergänzen — thematisch nach dem `IsDone vs IsDonePicking`-Eintrag einordnen:

```markdown
- **FA-Reconciliation: `IsCancelled` in ALLEN Offen-Queries (v1.25.0)**: `ProductionOrder.IsCancelled` (bool, Default false; + `CancelledAt`/`CancelledBy`, Migration 80) verhaelt sich wie `IsDone` — verwaiste FAs (in Sage geloescht) werden vom Service-Sync auf `IsCancelled=1` gesetzt und verschwinden aus offenen Sichten. **JEDE** Offen-Query MUSS `!IsCancelled` (bzw. `IsCancelled = 0`) fuehren — EF **und** Raw-SQL: `ProductionOrderRepository.GetOpenOrdersAsync`/`GetForLeitstandAsync(!showDone)`/`GetOpenOrdersInWindowAsync`, `FaCompletionController.Index`/`FaWorklistController.Index`, `FaWorkStepDetectionService` (beide Kandidaten-Queries), `BomCacheSyncService.ReadOpenOrdersInWindowAsync` (raw-SQL `whereClause`). Reine Entscheidung im unit-getesteten Helper `ProductionOrderReconciler.Plan` (Guard: leerer Sage-Read → Skip; Cap: `ToCancel > ReconcileMaxCancelPerRun` → Skip + Fehlermail, Reaktivierungen bleiben). Verdrahtung in `SageImportService.SyncProductionOrdersAsync` NACH dem Upsert, im selben SyncLog-Run, hinter Flag `Sync:ProductionOrderReconcileEnabled` (Default false); Counts-Keys `storniert`/`reaktiviert`. **Fallstrick:** Raw-SQL-Pfade (Reconcile-UPDATE, BomCache) sind NICHT InMemory-testbar — nur der `ProductionOrderReconciler`-Helper + die EF-Repo-/Detection-Queries sind es. Kein Reconcile im AgentJob (guard-loses `WHEN NOT MATCHED BY SOURCE` wuerde bei leerem/teilweisem View alle offenen FAs stornieren).
```

(b) In der **Service-Konfiguration (appsettings.json / ServiceSettings DB)**-Tabelle zwei Zeilen ergänzen (nach `Sync:FaWorkStepDetectionEnabled`):

```markdown
| `Sync:ProductionOrderReconcileEnabled` | `false` | Verwaiste FAs (in Sage geloescht) automatisch stornieren. Opt-in — Admin schaltet nach DryRun-Kontrolle scharf (v1.25.0) |
| `Sync:ReconcileMaxCancelPerRun` | `100` | Sicherheits-Cap: mehr Storno-Kandidaten je Lauf → kein Storno + Fehlermail (Schutz vor Sage-Teil-Reads) (v1.25.0) |
```

- [ ] **Step 3: TESTSZENARIEN — neues Kapitel inkl. DryRun-Kontroll-Lauf**

An `docs/TESTSZENARIEN.md` ein neues Kapitel anhängen (Kapitel-Nummer = nächste freie; prüfe mit `grep -n "^## Kapitel\|^## [0-9]" docs/TESTSZENARIEN.md | tail -3`):

```markdown
## Kapitel NN — FA-Reconciliation (verwaiste FAs stornieren) (v1.25.0)

**Vorbedingungen:**
- WMS-DB mit mindestens einer offenen FA (`IsDone=0`, `IsCancelled=0`), deren `OrderNumber` in der Sage-View `vw_AKE_Kommissionierung_WAListe` NICHT (mehr) vorkommt (= verwaiste FA).
- Service läuft, `Sync:ProductionOrdersEnabled=true`.
- DB-Backup vor dem ersten scharfen Lauf.

### 1. DryRun-Kontroll-Lauf (Flag AUS — nichts wird geschrieben)
1. `Sync:ProductionOrderReconcileEnabled = false` (Default), `WorkerSettings:SyncDryRun = false`.
2. Sync-Zyklus auslösen (oder Intervall abwarten).
3. **Erwartet:** Im Log/Aktivitäts-Protokoll erscheint „FA-Reconciliation deaktiviert … Plan: N Storno-Kandidaten, M Reaktivierungen (nichts geschrieben)". In der DB ist KEINE FA storniert. Der Admin liest N ab und prüft Plausibilität.

### 2. Scharfschalten — verwaiste FA wird storniert
1. `Sync:ProductionOrderReconcileEnabled = true`. `Sync:ReconcileMaxCancelPerRun` ausreichend hoch (Default 100).
2. Sync-Zyklus auslösen.
3. **Erwartet:** Die verwaiste FA hat `IsCancelled=1`, `CancelledAt`=jetzt, `CancelledBy='System-Reconcile'`. Aktivitäts-Protokoll-Counts zeigen `storniert=N`. Die FA verschwindet aus FA-Liste, Leitstand, FA-Vervollständigung, FA-Abarbeitungsliste, Picking-Worklist und aus dem BOM-Cache-Fenster.
4. FA-Liste mit „Erledigte anzeigen" öffnen → die FA erscheint mit rotem Badge **„In Sage gelöscht"** (nicht „erledigt").

### 3. Reaktivierung — FA taucht wieder in Sage auf
1. Die stornierte FA wieder in der Sage-View verfügbar machen (Testdaten).
2. Sync-Zyklus auslösen.
3. **Erwartet:** `IsCancelled=0`, `CancelledAt=NULL`, `CancelledBy=NULL`. Counts zeigen `reaktiviert=1`. FA ist wieder in den offenen Sichten.

### 4. Guard — leerer Sage-Read storniert NICHTS
1. Sage-View liefert (simuliert) 0 Zeilen (z. B. View temporär leer / Verbindungsproblem am Read).
2. Sync-Zyklus mit Flag AN auslösen.
3. **Erwartet:** KEINE FA wird storniert. Aktivitäts-Protokoll-Warnung „Reconcile übersprungen: Sage-Read leer". Keine Fehlermail (Guard ist kein Cap).

### 5. Cap — zu viele Storno-Kandidaten → kein Storno + Fehlermail
1. `Sync:ReconcileMaxCancelPerRun = 1`. Mehr als 1 verwaiste offene FA vorhanden.
2. `ErrorNotification:Enabled=true` + `ErrorNotification:Recipients` gesetzt.
3. Sync-Zyklus mit Flag AN auslösen.
4. **Erwartet:** KEINE FA storniert (Cap überschritten). Warnung im Protokoll „Reconcile übersprungen: Cap ueberschritten (N > 1)". Fehlermail an die Empfänger mit Sage-Count + Cap. Etwaige Reaktivierungen im selben Lauf werden trotzdem geschrieben.

**Negativ/Regression:**
- Erledigte FAs (`IsDone=1`), die nicht in Sage sind, werden NIE storniert (nur offene).
- Bereits stornierte FAs, die weiterhin fehlen, werden nicht erneut storniert (kein Doppel-Storno, Counts bleiben 0).
```

- [ ] **Step 4: PROJECT_STATUS.md**

In `PROJECT_STATUS.md` den v1.25.0-Abschnitt um einen Bullet ergänzen (Stil an bestehende Bullets angleichen):

```markdown
- **FA-Reconciliation (v1.25.0):** Service-Sync storniert in Sage gelöschte, offene FAs (`ProductionOrder.IsCancelled`, Migration 80); Guard + Cap + Reaktivierung + Fehlermail; Opt-in `Sync:ProductionOrderReconcileEnabled` (Default aus). Badge „In Sage gelöscht" in der FA-Liste.
```

- [ ] **Step 5: Build (Changelog-View kompiliert)**

Run: `dotnet build IdealAkeWms.slnx`
Expected: `Build succeeded.` `0 Error(s)`.

- [ ] **Step 6: Commit**

Run:
```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Views/Help/Changelog.cshtml CLAUDE.md docs/TESTSZENARIEN.md PROJECT_STATUS.md && git commit -m "$(cat <<'EOF'
docs: FA-Reconciliation — Changelog/CLAUDE/TESTSZENARIEN/PROJECT_STATUS (v1.25.0)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

## Task 10: Final-Check

**Files:** keine (Verifikation + ggf. Fix-Commit).

- [ ] **Step 1: Build 0 Fehler**

Run: `dotnet build IdealAkeWms.slnx`
Expected: `Build succeeded.` `0 Error(s)`, `0 Warning(s)` (bzw. nur unveränderte Bestands-Warnungen).

- [ ] **Step 2: PendingModelChanges leer**

Run:
```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet ef migrations has-pending-model-changes --project IdealAkeWms
```
Expected: „No changes …".

- [ ] **Step 3: Alle Tests grün**

Run: `dotnet test IdealAkeWms.Tests --nologo`
Expected: `Passed!` — inkl. der 4 neuen Repo-Tests.

Run: `dotnet test IDEALAKEWMSService.Tests --nologo`
Expected: `Passed!` — inkl. der 7 `ProductionOrderReconcilerTests` + `Detect_SkipsCancelledOrders`.

- [ ] **Step 4: grep-Beweise**

Run: `grep -rn "IsCancelled" IdealAkeWms/Models/ProductionOrder.cs SQL/00_FreshInstall.sql IDEALAKEWMSService/Services/ProductionOrderReconciler.cs IDEALAKEWMSService/Services/BomCacheSyncService.cs`
Expected: Treffer in allen vier Dateien (Model: 1 Property + 2 weitere Felder; FreshInstall: Schema-Spalte; Reconciler: Record-Feld + Filter; BomCache: `AND po.[IsCancelled] = 0`).

Run: `grep -c "storniert\|reaktiviert" IDEALAKEWMSService/Services/SageImportService.cs`
Expected: ≥ 2 (Counts-Keys).

Run: `grep -n "AddProductionOrderCancellation" SQL/00_FreshInstall.sql SQL/80_AddProductionOrderCancellation.sql`
Expected: History-Insert in FreshInstall + SQL/80 vorhanden (gleiche MigrationId).

- [ ] **Step 5: Working tree sauber**

Run: `cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git status --short`
Expected: leer (alle Änderungen committed). Falls Step 1-4 einen Fix erforderten, diesen committen:

```
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -m "$(cat <<'EOF'
chore: FA-Reconciliation Final-Check Fixes (v1.25.0)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

## File Structure (Übersicht nach Abschluss)

```
IdealAkeWms/
  Models/ProductionOrder.cs                          (M) +IsCancelled/CancelledAt/CancelledBy
  Data/ApplicationDbContext.cs                       (M) CancelledBy MaxLength(256) + IX_IsCancelled
  Data/Repositories/IProductionOrderRepository.cs    (M) LeitstandOrderRow +IsCancelled
  Data/Repositories/ProductionOrderRepository.cs     (M) 3 Offen-Queries +!IsCancelled + Projection
  Controllers/ProductionOrdersController.cs          (M) IsCancelled ins ViewItem
  Controllers/FaCompletionController.cs              (M) Offen-Filter +!IsCancelled
  Controllers/FaWorklistController.cs                (M) Offen-Filter +!IsCancelled
  Models/ViewModels/ProductionOrderListViewModel.cs  (M) ProductionOrderListItem.IsCancelled
  Views/ProductionOrders/Index.cshtml                (M) Badge "In Sage gelöscht"
  Views/Help/Changelog.cshtml                        (M) v1.25.0-Card-Bullet
  Migrations/<ts>_AddProductionOrderCancellation.cs  (N) EF-Migration

IDEALAKEWMSService/
  Services/ProductionOrderReconciler.cs              (N) reiner Helper (Record + Plan)
  Services/SageImportService.cs                      (M) Reconcile-Wiring + ISyncErrorNotifier
  Services/BomCacheSyncService.cs                    (M) raw-SQL AND IsCancelled = 0
  Services/FaWorkStepDetectionService.cs             (M) beide Kandidaten-Queries +!IsCancelled
  appsettings.json                                   (M) 2 Sync:-Keys

IDEALAKEWMSService.Tests/
  Services/ProductionOrderReconcilerTests.cs         (N) 7 Unit-Tests
  Services/SageImportServiceTests.cs                 (M) Build-Helper +Notifier-Mock
  Services/FaWorkStepDetectionServiceTests.cs        (M) Detect_SkipsCancelledOrders

IdealAkeWms.Tests/
  Repositories/ProductionOrderRepositoryTests.cs     (M) 4 IsCancelled-Tests

SQL/
  80_AddProductionOrderCancellation.sql              (N) idempotentes Deploy-Skript
  00_FreshInstall.sql                                (M) 3 Spalten + Index + History-Insert

docs/TESTSZENARIEN.md                                (M) Kapitel NN (5 Szenarien + DryRun)
CLAUDE.md                                            (M) Fallstrick + ServiceSettings-Tabelle
PROJECT_STATUS.md                                    (M) v1.25.0-Bullet
```

**Task-Reihenfolge / Abhängigkeiten:** 0 → 1 → 2 (Migration braucht Modell) → 3 (Helper, unabhängig) → 4 (EF-Queries, braucht Task 1) → 5 (Service-Queries, braucht Task 1) → 6 (Verdrahtung, braucht Task 3+1) → 7 (Config, braucht Task 6-Referenzen) → 8 (UI, braucht Task 4-ViewModel) → 9 (Doku) → 10 (Final). Tasks 3, 4, 5 sind untereinander nur über Task 1 gekoppelt — nach Task 2 könnten 3/4/5 theoretisch parallel laufen, hier aber sequenziell geplant (kleiner Diff, gemeinsamer Build).
