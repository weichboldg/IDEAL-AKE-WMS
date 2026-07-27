# FA-AG-Erkennung + BOM-Cache: Aktivitäts-Protokoll granular — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Das Aktivitäts-Protokoll der FA-AG-Erkennungs-Pipeline granular aufgliedern — (A) `FaWorkStepDetection`: nicht gefundene Suchbegriffe + je neu erkanntem FA der auslösende Begriff/AG; (B) `BomCache`: wie viele offene FAs der Cap rausdrängt + Artikel ohne BOM-Daten.

**Architecture:** Reines Logging, kein DB-/Migrations-/Auswahl-Eingriff. Teil A baut `FaWorkStepDetectionService.DetectAsync` (EF, InMemory-testbar) um: pro Begriff getroffene Artikel führen → Counts + Nicht-Treffer-Liste (in `messageSuffix`) + Info-Detailzeile je neu erkanntem FA. Teil B extrahiert die Abdeckungs-Auswertung in einen reinen Helper `BomCacheCoverage` (unit-testbar) und verdrahtet ihn in `BomCacheSyncService` (raw-SQL → Manual-UAT). Der Test-Fake `FakeSyncRun` wird minimal um die Erfassung von `messageSuffix` erweitert.

**Tech Stack:** .NET 10 Windows-Service (`IDEALAKEWMSService`), EF Core 10 + raw ADO.NET, xUnit + FluentAssertions + EF InMemory + `FakeSyncLogger`.

**Branch / Worktree:** `feature/windows-auth-ad-users` im Worktree `.claude/worktrees/missingparts-include-pd`. Spec: [docs/superpowers/specs/2026-06-25-fa-detection-protokoll-aufgliederung-design.md](../specs/2026-06-25-fa-detection-protokoll-aufgliederung-design.md).

**WICHTIG (Standing Constraints):** KEIN Merge nach `main`, KEIN Worktree-/Branch-Cleanup ohne ausdrückliche User-Freigabe. Plan endet mit grünem Build + Tests auf dem Branch.

---

## File Structure

**Neu:**
- `IDEALAKEWMSService/Services/BomCacheCoverage.cs` — reine Abdeckungs-Auswertung (Counts + Warn-Strings).
- `IDEALAKEWMSService.Tests/Services/BomCacheCoverageTests.cs` — Unit-Tests dazu.

**Geändert:**
- `IdealAkeWms.Tests/Helpers/FakeSyncLogger.cs` — `FinalMessageSuffix` erfassen.
- `IDEALAKEWMSService/Services/FaWorkStepDetectionService.cs` — granulares Logging.
- `IDEALAKEWMSService.Tests/Services/FaWorkStepDetectionServiceTests.cs` — neue Tests.
- `IDEALAKEWMSService/Services/BomCacheSyncService.cs` — Coverage-Wiring.
- `IdealAkeWms/Views/Help/Changelog.cshtml`, `Views/Help/Index.cshtml`, `CLAUDE.md`, `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md` — Doku.

---

## Task 1: `FakeSyncRun` erfasst `messageSuffix`

**Files:**
- Modify: `IdealAkeWms.Tests/Helpers/FakeSyncLogger.cs:54-62`

- [ ] **Step 1: Property + Speicherung ergänzen**

In `IdealAkeWms.Tests/Helpers/FakeSyncLogger.cs` in `FakeSyncRun` nach Zeile 28 (`public string? FinalErrorMessage { get; private set; }`) einfügen:

```csharp
    public string? FinalMessageSuffix { get; private set; }
```

Und in `FinishSuccessAsync` (Zeile 57-61) die Zuweisung ergänzen:

```csharp
        if (FinishedSuccess || FinishedFailed) return Task.CompletedTask;
        FinishedSuccess = true;
        FinalCounts = counts;
        FinalMessageSuffix = messageSuffix;
        return Task.CompletedTask;
```

- [ ] **Step 2: Build to verify compile**

Run: `dotnet build IdealAkeWms.Tests/IdealAkeWms.Tests.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms.Tests/Helpers/FakeSyncLogger.cs
git commit -m "test(synclog): FakeSyncRun erfasst messageSuffix"
```

---

## Task 2: `FaWorkStepDetectionService` granular (TDD)

**Files:**
- Modify: `IDEALAKEWMSService/Services/FaWorkStepDetectionService.cs:32-123` (`DetectAsync`)
- Test: `IDEALAKEWMSService.Tests/Services/FaWorkStepDetectionServiceTests.cs`

> Hinweis: Die Tests nutzen die in der Datei vorhandenen Seed-Helfer `NewWorkStep(code, searchString)`, `NewOrder(orderNumber, articleNumber)`, `NewBomHeader(artikelnummer, params bezeichnungen)` und `CreateService(ctx, fakeLogger)` (siehe bestehende Tests). Falls eine Helper-Signatur leicht abweicht, an die tatsächliche anpassen — die Assertions prüfen Substrings unabhängig von `WorkStep.Name`.

- [ ] **Step 1: Failing tests schreiben**

Am Ende der `FaWorkStepDetectionServiceTests`-Klasse (vor schließender `}`) einfügen:

```csharp
    [Fact]
    public async Task Detect_TermWithoutHit_CountedAndListedInSuffix()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.WorkSteps.Add(NewWorkStep("VX", "gibtsnichtimbom"));
        ctx.CachedBomHeaders.Add(NewBomHeader("ART-1", "Irgendein Teil"));
        ctx.ProductionOrders.Add(NewOrder("FA-1", "ART-1"));
        await ctx.SaveChangesAsync();

        var fake = new FakeSyncLogger();
        await CreateService(ctx, fake).DetectAsync(dryRun: false);

        var run = fake.Runs.Single();
        run.FinalCounts!["suchbegriffe gesamt"].Should().Be(1);
        run.FinalCounts!["mit treffer"].Should().Be(0);
        run.FinalCounts!["ohne treffer"].Should().Be(1);
        run.FinalMessageSuffix.Should().Contain("Ohne Treffer:").And.Contain("gibtsnichtimbom (VX)");
    }

    [Fact]
    public async Task Detect_NewlyDetectedFa_WritesInfoLineWithTermAndOrderNumber()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.WorkSteps.Add(NewWorkStep("VL", "luefter"));
        ctx.CachedBomHeaders.Add(NewBomHeader("ART-1", "Axialluefter 230V"));
        ctx.ProductionOrders.Add(NewOrder("FA-1", "ART-1"));
        await ctx.SaveChangesAsync();

        var fake = new FakeSyncLogger();
        await CreateService(ctx, fake).DetectAsync(dryRun: false);

        var run = fake.Runs.Single();
        var info = run.Events.Where(e => e.Level == "Info").ToList();
        info.Should().ContainSingle(e =>
            e.Message.Contains("FA FA-1")
            && e.Message.Contains("AG VL")
            && e.Message.Contains("Begriff: luefter")
            && e.Reference == "FA-1");
        run.FinalCounts!["neu"].Should().Be(1);
        run.FinalCounts!["mit treffer"].Should().Be(1);
    }

    [Fact]
    public async Task Detect_AlreadyAssignedFa_WritesNoInfoLine()
    {
        using var ctx = TestDbContextFactory.Create();
        var step = NewWorkStep("VL", "luefter");
        var order = NewOrder("FA-1", "ART-1");
        ctx.WorkSteps.Add(step);
        ctx.CachedBomHeaders.Add(NewBomHeader("ART-1", "Axialluefter 230V"));
        ctx.ProductionOrders.Add(order);
        await ctx.SaveChangesAsync();
        ctx.FaWorkSteps.Add(new FaWorkStep
        {
            ProductionOrderId = order.Id, WorkStepId = step.Id, Source = FaWorkStepSources.Sync,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
        });
        await ctx.SaveChangesAsync();

        var fake = new FakeSyncLogger();
        await CreateService(ctx, fake).DetectAsync(dryRun: false);

        var run = fake.Runs.Single();
        run.Events.Where(e => e.Level == "Info").Should().BeEmpty();
        run.FinalCounts!["neu"].Should().Be(0);
        run.FinalCounts!["uebersprungen"].Should().Be(1);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~Detect_TermWithoutHit|FullyQualifiedName~Detect_NewlyDetectedFa|FullyQualifiedName~Detect_AlreadyAssignedFa"`
Expected: FAIL — die neuen Counts/`FinalMessageSuffix`/Info-Zeilen existieren noch nicht (`suchbegriffe gesamt` KeyNotFound bzw. keine Info-Events).

- [ ] **Step 3: `DetectAsync` umbauen**

In `IDEALAKEWMSService/Services/FaWorkStepDetectionService.cs` die komplette `DetectAsync`-Methode (Zeile 32-123) ersetzen durch:

```csharp
    public async Task<SyncResult> DetectAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.FaWorkStepDetection, ct);
        try
        {
            var steps = await _db.WorkSteps
                .Where(w => w.IsActive && w.SearchString != null && w.SearchString != "")
                .ToListAsync(ct);

            int added = 0, skipped = 0;
            int termsTotal = 0, termsWithHit = 0;
            var termsWithoutHit = new List<string>(); // "begriff (Code)"

            foreach (var step in steps)
            {
                ct.ThrowIfCancellationRequested();

                var terms = step.SearchString!
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToLowerInvariant())
                    .Distinct()
                    .ToList();
                if (terms.Count == 0) continue;

                // Pro Begriff getroffene Artikel — fuer Counts + welcher Begriff je Artikel ausloeste.
                var stepMatchedArticles = new HashSet<string>(StringComparer.Ordinal);
                var articleToTerms = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
                foreach (var term in terms)
                {
                    termsTotal++;
                    var arts = await _db.CachedBomItems
                        .Where(i => (i.Bezeichnung1 != null && i.Bezeichnung1.ToLower().Contains(term))
                                 || (i.Bezeichnung2 != null && i.Bezeichnung2.ToLower().Contains(term)))
                        .Select(i => i.CachedBomHeader!.Artikelnummer)
                        .Distinct()
                        .ToListAsync(ct);

                    if (arts.Count == 0)
                    {
                        termsWithoutHit.Add($"{term} ({step.Code})");
                        continue;
                    }
                    termsWithHit++;
                    foreach (var a in arts)
                    {
                        stepMatchedArticles.Add(a);
                        if (!articleToTerms.TryGetValue(a, out var set))
                        {
                            set = new SortedSet<string>(StringComparer.Ordinal);
                            articleToTerms[a] = set;
                        }
                        set.Add(term);
                    }
                }
                if (stepMatchedArticles.Count == 0) continue;

                var matchedFaCount = await _db.ProductionOrders
                    .Where(o => !o.IsDone
                             && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking)
                             && o.ArticleNumber != null && stepMatchedArticles.Contains(o.ArticleNumber!))
                    .CountAsync(ct);
                var candidates = await _db.ProductionOrders
                    .Where(o => !o.IsDone
                             && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking)
                             && o.ArticleNumber != null && stepMatchedArticles.Contains(o.ArticleNumber!))
                    .Where(o => !_db.FaWorkSteps.Any(f => f.ProductionOrderId == o.Id && f.WorkStepId == step.Id))
                    .Select(o => new { o.Id, o.OrderNumber, o.ArticleNumber })
                    .ToListAsync(ct);

                skipped += matchedFaCount - candidates.Count; // Zeile existiert bereits (aktiv oder IsRemoved)
                foreach (var cand in candidates)
                {
                    if (!dryRun)
                    {
                        _db.FaWorkSteps.Add(new FaWorkStep
                        {
                            ProductionOrderId = cand.Id,
                            WorkStepId = step.Id,
                            Source = FaWorkStepSources.Sync,
                            CreatedAt = DateTime.Now,
                            CreatedBy = "FaWorkStepDetection",
                            CreatedByWindows = "FaWorkStepDetection",
                        });
                    }
                    added++;

                    var triggers = (cand.ArticleNumber != null && articleToTerms.TryGetValue(cand.ArticleNumber, out var ts))
                        ? string.Join(", ", ts)
                        : "";
                    await run.LogInfoAsync(
                        $"FA {cand.OrderNumber} → AG {step.Code} {step.Name} erkannt (Begriff: {triggers})",
                        reference: cand.OrderNumber, ct: ct);
                }
            }

            if (!dryRun) await _db.SaveChangesAsync(ct);

            // Lauf-Zusammenfassung: optional [DryRun] + kompakte Nicht-Treffer-Liste (1 Zeile/Lauf, Cap 50).
            var suffixParts = new List<string>();
            if (dryRun) suffixParts.Add("[DryRun]");
            if (termsWithoutHit.Count > 0)
            {
                const int cap = 50;
                var shown = termsWithoutHit.Take(cap).ToList();
                var extra = termsWithoutHit.Count - shown.Count;
                suffixParts.Add($"Ohne Treffer: {string.Join(", ", shown)}{(extra > 0 ? $" … (+{extra} weitere)" : "")}");
            }
            var messageSuffix = suffixParts.Count > 0 ? string.Join(" — ", suffixParts) : null;

            _logger.LogInformation(
                "FA-Arbeitsgang-Erkennung abgeschlossen: {Added} neu, {Skipped} uebersprungen, " +
                "{TermsTotal} Begriffe ({WithHit} mit Treffer, {NoHit} ohne){DryRun}",
                added, skipped, termsTotal, termsWithHit, termsTotal - termsWithHit, dryRun ? " [DryRun]" : "");

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = added,
                ["uebersprungen"] = skipped,
                ["suchbegriffe gesamt"] = termsTotal,
                ["mit treffer"] = termsWithHit,
                ["ohne treffer"] = termsTotal - termsWithHit,
            }, messageSuffix: messageSuffix, ct: ct);

            return new SyncResult(added, 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei FA-Arbeitsgang-Erkennung.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~FaWorkStepDetectionServiceTests"`
Expected: PASS (bestehende + 3 neue Tests grün).

- [ ] **Step 5: Commit**

```bash
git add IDEALAKEWMSService/Services/FaWorkStepDetectionService.cs IDEALAKEWMSService.Tests/Services/FaWorkStepDetectionServiceTests.cs
git commit -m "feat(detection): granulares Protokoll — nicht gefundene Begriffe + Begriff je erkanntem FA"
```

---

## Task 3: `BomCacheCoverage` Helper (TDD)

**Files:**
- Create: `IDEALAKEWMSService/Services/BomCacheCoverage.cs`
- Test: `IDEALAKEWMSService.Tests/Services/BomCacheCoverageTests.cs`

- [ ] **Step 1: Failing tests schreiben**

`IDEALAKEWMSService.Tests/Services/BomCacheCoverageTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class BomCacheCoverageTests
{
    [Fact]
    public void Build_CapHit_SetsCountsAndCapWarning()
    {
        var r = BomCacheCoverage.Build(totalEligibleFas: 643, cachedFas: 500, cap: 500,
            articlesWithoutBom: new List<string>());

        r.Counts["fa im fenster"].Should().Be(643);
        r.Counts["fa gecacht"].Should().Be(500);
        r.Counts["artikel ohne bom"].Should().Be(0);
        r.CapWarning.Should().NotBeNull();
        r.CapWarning.Should().Contain("143 FAs ohne Cache-Eintrag");
        r.NoBomWarning.Should().BeNull();
    }

    [Fact]
    public void Build_WithinCap_NoCapWarning()
    {
        var r = BomCacheCoverage.Build(50, 50, 500, new List<string>());
        r.CapWarning.Should().BeNull();
    }

    [Fact]
    public void Build_ArticlesWithoutBom_SetsWarningAndCount()
    {
        var r = BomCacheCoverage.Build(50, 50, 500, new List<string> { "ART-A", "ART-B" });
        r.Counts["artikel ohne bom"].Should().Be(2);
        r.NoBomWarning.Should().Contain("ART-A").And.Contain("ART-B");
    }

    [Fact]
    public void Build_ManyArticlesWithoutBom_CapsListWithRemainder()
    {
        var many = Enumerable.Range(1, 40).Select(i => $"ART{i}").ToList();
        var r = BomCacheCoverage.Build(10, 10, 500, many);
        r.NoBomWarning.Should().Contain("(+10 weitere)"); // 40 - Cap 30
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~BomCacheCoverageTests"`
Expected: FAIL — `BomCacheCoverage` existiert nicht.

- [ ] **Step 3: Helper implementieren**

`IDEALAKEWMSService/Services/BomCacheCoverage.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Reine Auswertung der BOM-Cache-Abdeckung fuers Aktivitaets-Protokoll: Zusatz-Zaehler +
/// optionale Warn-Texte. Keine DB/IO -> unit-testbar (der raw-SQL-Teil des
/// BomCacheSyncService bleibt Manual-UAT).
/// </summary>
public static class BomCacheCoverage
{
    /// <summary>Max. Anzahl Artikelnummern in der "ohne BOM-Daten"-Liste.</summary>
    public const int NoBomListCap = 30;

    public static BomCacheCoverageResult Build(
        int totalEligibleFas, int cachedFas, int cap,
        IReadOnlyList<string> articlesWithoutBom)
    {
        var counts = new Dictionary<string, int>
        {
            ["fa im fenster"] = totalEligibleFas,
            ["fa gecacht"] = cachedFas,
            ["artikel ohne bom"] = articlesWithoutBom.Count,
        };

        string? capWarning = null;
        if (totalEligibleFas > cap)
        {
            var diff = totalEligibleFas - cachedFas;
            capWarning =
                $"Cap erreicht: {cachedFas} von {totalEligibleFas} offenen FAs gecacht (Cap {cap}) — " +
                $"{diff} FAs ohne Cache-Eintrag, werden NICHT automatisch erkannt.";
        }

        string? noBomWarning = null;
        if (articlesWithoutBom.Count > 0)
        {
            var shown = articlesWithoutBom.Take(NoBomListCap).ToList();
            var extra = articlesWithoutBom.Count - shown.Count;
            noBomWarning = $"Artikel ohne BOM-Daten (SAGE+OSEON leer): {string.Join(", ", shown)}"
                + (extra > 0 ? $" … (+{extra} weitere)" : "");
        }

        return new BomCacheCoverageResult(counts, capWarning, noBomWarning);
    }
}

public sealed record BomCacheCoverageResult(
    IReadOnlyDictionary<string, int> Counts,
    string? CapWarning,
    string? NoBomWarning);
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~BomCacheCoverageTests"`
Expected: PASS (4 Tests grün).

- [ ] **Step 5: Commit**

```bash
git add IDEALAKEWMSService/Services/BomCacheCoverage.cs IDEALAKEWMSService.Tests/Services/BomCacheCoverageTests.cs
git commit -m "feat(bomcache): BomCacheCoverage-Helper (Cap-/BOM-Abdeckung) + Tests"
```

---

## Task 4: `BomCacheCoverage` in `BomCacheSyncService` verdrahten

**Files:**
- Modify: `IDEALAKEWMSService/Services/BomCacheSyncService.cs` (`ReadOpenOrdersInWindowAsync` Zeile 376-412; `SyncBomCacheAsync` Zeile 49 + Loop Zeile 89-129 + FinishSuccess Zeile 143-148)

> Der Pfad nutzt raw ADO.NET → nicht InMemory-testbar (Manual-UAT). Die testbare Logik liegt in `BomCacheCoverage` (Task 3). Hier nur Verdrahtung + Build.

- [ ] **Step 1: `ReadOpenOrdersInWindowAsync` liefert zusätzlich die Gesamtzahl**

In `IDEALAKEWMSService/Services/BomCacheSyncService.cs` die komplette Methode `ReadOpenOrdersInWindowAsync` (Zeile 376-412) ersetzen durch:

```csharp
    private async Task<(List<(int OrderId, string ArticleNumber)> Orders, int TotalEligible)> ReadOpenOrdersInWindowAsync(
        int weeksAhead, int maxOrders, CancellationToken ct)
    {
        var connStr = ConnectionStrings.Wms(_configuration);
        var orders = new List<(int, string)>();

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        // "Abgeschlossen" = IsDone (Sage) ODER IsDonePicking (App). Komm-abgeschlossene FAs
        // (IsDone=0, IsDonePicking=1) duerfen das Fenster NICHT belegen (Web-Semantik v1.21.1).
        const string whereClause = @"
            WHERE po.[IsDone] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[ProductionOrderPickingStatus] ps
                  WHERE ps.[ProductionOrderId] = po.[Id] AND ps.[IsDonePicking] = 1
              )
              AND po.[ProductionDate] IS NOT NULL
              AND po.[ProductionDate] <= DATEADD(week, @weeks, GETDATE())
              AND po.[ArticleNumber] IS NOT NULL";

        // 1) Gesamtzahl eignungsfaehiger offener FAs (OHNE TOP) — fuer die Cap-Abdeckung.
        int totalEligible;
        await using (var countCmd = new SqlCommand(
            $"SELECT COUNT(*) FROM [dbo].[ProductionOrders] po {whereClause}", conn) { CommandTimeout = 60 })
        {
            countCmd.Parameters.AddWithValue("@weeks", weeksAhead);
            totalEligible = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));
        }

        // 2) TOP(@max)-Auswahl (unveraendert).
        var sql = $@"
            SELECT TOP (@max) po.[Id], po.[ArticleNumber]
            FROM [dbo].[ProductionOrders] po
            {whereClause}
            ORDER BY po.[ProductionDate] ASC";

        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        cmd.Parameters.AddWithValue("@max", maxOrders);
        cmd.Parameters.AddWithValue("@weeks", weeksAhead);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            orders.Add((reader.GetInt32(0), reader.GetString(1)));
        }
        return (orders, totalEligible);
    }
```

- [ ] **Step 2: Aufruf + Tracking in `SyncBomCacheAsync`**

In `SyncBomCacheAsync` die Zeile 49 (`var orders = await ReadOpenOrdersInWindowAsync(weeksAhead, maxOrders, ct);`) ersetzen durch:

```csharp
            var (orders, totalEligibleFas) = await ReadOpenOrdersInWindowAsync(weeksAhead, maxOrders, ct);
            var articlesWithoutBom = new List<string>();
```

Im Artikel-Loop den BOM-leer-Zweig (Zeile 93-97) so erweitern, dass leere Artikel mitgezählt werden:

```csharp
                if (!sageData.TryGetValue(art, out var items) || items.Count == 0)
                {
                    articlesWithoutBom.Add(art);
                    _logger.LogDebug("BOM-Cache-Sync: Keine BOM-Daten fuer {Article}", art);
                    continue;
                }
```

- [ ] **Step 3: Coverage bauen + Warnungen loggen + Counts mergen**

In `SyncBomCacheAsync` den `FinishSuccessAsync`-Block (Zeile 143-148) ersetzen durch:

```csharp
            var coverage = BomCacheCoverage.Build(totalEligibleFas, orders.Count, maxOrders, articlesWithoutBom);
            if (coverage.CapWarning != null) await run.LogWarningAsync(coverage.CapWarning, ct: ct);
            if (coverage.NoBomWarning != null) await run.LogWarningAsync(coverage.NoBomWarning, ct: ct);

            var finalCounts = new Dictionary<string, int>
            {
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["uebersprungen"] = skipped,
            };
            foreach (var kv in coverage.Counts) finalCounts[kv.Key] = kv.Value;

            await run.FinishSuccessAsync(finalCounts, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
```

> Der frühe Erfolgs-Abschluss bei `articleNumbers.Count == 0` (Zeile 59-69) bleibt unverändert — das ist der „keine offenen FAs"-Fall (totalEligible = 0, keine Warnung nötig).

- [ ] **Step 4: Build**

Run: `dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add IDEALAKEWMSService/Services/BomCacheSyncService.cs
git commit -m "feat(bomcache): Cache-Abdeckung protokollieren (Cap-/BOM-leer-Warnung + Counts)"
```

---

## Task 5: Doku

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`, `Views/Help/Index.cshtml`, `CLAUDE.md`, `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md`

- [ ] **Step 1: Changelog-Bullet**

In `IdealAkeWms/Views/Help/Changelog.cshtml` in der **v1.23.0**-Karte am Ende der `<ul>` (vor `</ul>`) ergänzen:

```razor
                    <li><strong>FA-AG-Erkennung im Aktivitäts-Protokoll aufgeschlüsselt:</strong>
                        Der Erkennungs-Lauf zeigt jetzt nicht gefundene Suchbegriffe und je neu
                        erkanntem FA, welcher Begriff/AG ausgelöst hat. Der BOM-Cache-Lauf warnt,
                        wenn mehr offene FAs anstehen als der Cache fasst (Cap erreicht → diese FAs
                        werden nicht automatisch erkannt) bzw. wenn Artikel keine Stückliste liefern.</li>
```

- [ ] **Step 2: CLAUDE.md — Fallstrick ergänzen**

In `CLAUDE.md` den bestehenden Fallstrick „FaWorkStep-Detection haengt NICHT am BomCache-ContentHash-Pfad" am Ende um folgenden Satz erweitern:

```
**Protokoll-Aufgliederung (seit v1.23.0):** Der `FaWorkStepDetection`-Lauf loggt zusätzlich Counts `suchbegriffe gesamt/mit treffer/ohne treffer`, eine kompakte „Ohne Treffer: begriff (Code), …"-Liste in der Lauf-Message (Cap 50) und je NEU erkanntem FA eine Info-Zeile `FA <Nr> → AG <Code> <Name> erkannt (Begriff: <terms>)` (nur Neu-Erkennungen → kein Spam). Der `BomCache`-Lauf loggt Counts `fa im fenster/fa gecacht/artikel ohne bom` und WARNT bei `fa im fenster > Cap` (Cap erreicht → FAs ohne Cache-Eintrag werden NICHT automatisch erkannt) sowie bei Artikeln ohne BOM-Daten. Reine Auswertung in `BomCacheCoverage` (unit-testbar); die raw-SQL-Count ist Manual-UAT. „Ohne Treffer" / „nicht im Cache" heißt NICHT zwingend „Fehler" — kann am Cache-Fenster/Cap liegen (Info bei Detection, Warning bei BomCache-Cap).
```

- [ ] **Step 3: Help/Index.cshtml — kurzer Hinweis**

In `IdealAkeWms/Views/Help/Index.cshtml` im Abschnitt zum Aktivitäts-Protokoll / der FA-Erkennung (passende Stelle suchen — z. B. wo `FaWorkStepDetection`/BOM-Cache erklärt wird; falls kein eigener Abschnitt existiert, ans Ende des Aktivitäts-Protokoll-Absatzes) einen Satz ergänzen:

```html
<p>
    Im Aktivitäts-Protokoll zeigt der Eintrag <strong>FaWorkStepDetection</strong> nicht
    gefundene Suchbegriffe und je neu erkanntem Auftrag den auslösenden Begriff. Der Eintrag
    <strong>BomCache</strong> warnt, wenn mehr offene Aufträge anstehen als der Cache fasst
    (dann werden überzählige Aufträge nicht automatisch erkannt — Cache-Obergrenze in den
    Service-Einstellungen erhöhen).
</p>
```

- [ ] **Step 4: TESTSZENARIEN — neues Kapitel**

In `docs/TESTSZENARIEN.md` direkt vor der `*Ende des Dokuments*`-Fußzeile (und dem `---` davor) einfügen (höchstes vorhandenes Kapitel ist 43 → neues Kapitel **44**):

```markdown
## Kapitel 44: FA-AG-Erkennung + BOM-Cache — Protokoll-Aufgliederung (v1.23.0)

**Vorbedingung:** `Sync:BomCacheEnabled` + `Sync:FaWorkStepDetectionEnabled` aktiv. WorkSteps mit
Suchbegriffen gepflegt. Service-Lauf auslösen (oder Neustart).

### TS-44.1 Nicht gefundene Suchbegriffe
1. Einen Suchbegriff pflegen, der in keiner gecachten Stückliste vorkommt (z. B. Tippfehler).
2. Lauf abwarten → `/SyncLog` → Eintrag `FaWorkStepDetection`.
   - **Erwartet:** Counts `ohne treffer` ≥ 1; in der Lauf-Message „Ohne Treffer: <begriff> (<Code>)".

### TS-44.2 Begriff je erkanntem FA
1. Einen offenen FA mit gecachter Stückliste, dessen BOM einen Suchbegriff enthält, neu erkennen lassen.
   - **Erwartet:** `FaWorkStepDetection`-Detailzeilen `FA <Nr> → AG <Code> <Name> erkannt (Begriff: <term>)`.
2. Nächster Lauf (nichts Neues) → **keine** neuen Detailzeilen (kein Spam).

### TS-44.3 BOM-Cache Cap-Warnung
1. `Sync:BomCacheMaxOrders` kleiner setzen als die Zahl offener FAs im Fenster.
2. Lauf abwarten → `/SyncLog` → Eintrag `BomCache`.
   - **Erwartet:** Counts `fa im fenster` > `fa gecacht`; Warn-Zeile „Cap erreicht: X von Y … Z FAs
     ohne Cache-Eintrag, werden NICHT automatisch erkannt."
3. Cap groß genug setzen → nächster Lauf: keine Cap-Warnung; die zuvor fehlende FA wird gecacht und erkannt.
```

- [ ] **Step 5: PROJECT_STATUS.md**

In `PROJECT_STATUS.md` im v1.23.0-Abschnitt einen Bullet ergänzen:

```markdown
- FA-AG-Erkennungs-Pipeline im Aktivitäts-Protokoll aufgeschlüsselt: `FaWorkStepDetection`
  (nicht gefundene Begriffe + Begriff je erkanntem FA), `BomCache` (Cap-/BOM-Abdeckungs-Warnung
  via `BomCacheCoverage`-Helper). Reines Logging, kein Migrations-Eingriff.
```

- [ ] **Step 6: Build + Commit**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

```bash
git add IdealAkeWms/Views/Help/Changelog.cshtml IdealAkeWms/Views/Help/Index.cshtml CLAUDE.md docs/TESTSZENARIEN.md PROJECT_STATUS.md
git commit -m "docs(v1.23.0): FA-AG-Erkennung + BOM-Cache Protokoll-Aufgliederung"
```

---

## Task 6: Final-Check

**Files:** keine.

- [ ] **Step 1: Voller Build (Service + Web + Tests)**

Run: `dotnet build IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj`
Run: `dotnet build IdealAkeWms.Tests/IdealAkeWms.Tests.csproj`
Expected: beide Build succeeded, 0 Errors.

- [ ] **Step 2: Volle Testsuiten**

Run: `dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj`
Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj`
Expected: beide grün (Service: bestehende + 3 Detection + 4 Coverage; Web: 770 + 0 unverändert, keine Regression durch die FakeSyncLogger-Erweiterung).

> **STOP — keine Merge-/Cleanup-Aktion.** Plan endet hier. Merge/Cleanup nur auf ausdrückliche User-Freigabe. (Unabhängig davon: nach dem nächsten echten Service-Lauf die `BomCache`-Counts prüfen, ob der 500er-Cap reicht — sonst separater Bugfix an der `ORDER BY ASC`-Auswahl.)

---

## Self-Review (vom Plan-Autor)

**Spec-Coverage:**
- Spec §3 (Teil A: Counts, Nicht-Treffer-Liste in messageSuffix, per-FA Info-Zeile, DryRun) → Task 1 (Fake) + Task 2. ✅
- Spec §4 (Teil B: Zusatz-Zähler, Cap-Warnung, Artikel-ohne-BOM, Helper-Extraktion) → Task 3 (Helper+Tests) + Task 4 (Wiring). ✅
- Spec §5 Vokabular (`suchbegriffe gesamt/mit treffer/ohne treffer`, `fa im fenster/fa gecacht/artikel ohne bom`) → Task 2 + Task 3 exakt diese Keys. ✅
- Spec §6 Tests (Detection InMemory; Helper pure) → Task 2 + Task 3. ✅
- Spec §7 Doku → Task 5. ✅

**Placeholder-Scan:** Einziger weiche Punkt — Task 2 nutzt bestehende Seed-Helfer (`NewWorkStep`/`NewOrder`/`NewBomHeader`), deren exakte Signatur aus den bestehenden Tests stammt; Assertions sind substring-basiert (unabhängig von `WorkStep.Name`), daher robust gegen kleine Helper-Abweichungen. Kein Vagheits-Platzhalter.

**Typ-Konsistenz:** `BomCacheCoverage.Build(int, int, int, IReadOnlyList<string>) → BomCacheCoverageResult(Counts, CapWarning, NoBomWarning)` identisch in Helper (Task 3) und Wiring (Task 4). `ReadOpenOrdersInWindowAsync` neuer Rückgabetyp `(List<(int,string)> Orders, int TotalEligible)` konsistent mit dem Aufrufer (`var (orders, totalEligibleFas) = …`). Counts-Keys wörtlich konsistent zwischen Service, Helper und Tests. `FakeSyncRun.FinalMessageSuffix` (Task 1) wird in Task 2-Tests gelesen.
