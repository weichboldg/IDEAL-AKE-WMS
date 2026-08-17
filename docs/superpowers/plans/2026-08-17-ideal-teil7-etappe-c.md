# IDEAL Teil 7 — Etappe C: Materialisierungs-Sync + drei Sync-Regeln — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Die lokale FA-Struktur (`FaHierarchyNode`, Teil 1) nach `ProductionOrders` materialisieren — reine, unit-getestete Sync-Regeln (anlegen / nicht löschen + `SageMissingSince` selbstheilend / Umhängung melden) plus ein Service-Orchestrator, gated auf den Master `ProduktionsauftragHierarchisch`.

**Architecture:** Reiner Planer `FaMaterializationPlanner.Plan(source, existing)` (Muster `ProductionOrderReconciler`, keine DB, empty-source-Guard) liefert ToCreate/ToClearMissing/ToMarkMissing/ReparentConflicts/MissingToNotify. Orchestrator `FaMaterializationSyncService` liest lokale `FaHierarchyNodes`, mappt int→string (SubOrderNumber=SubFA, OrderNumber=HauptFA, ParentSubOrderNumber=VaterFA), wendet den Plan per EF an (App-verwaltete Felder erhalten), schickt **eine** Sammelmail pro Lauf und protokolliert (`ISyncLogger`, Service `FaMaterialization`). Läuft nur bei Master=true, nach dem FA-Hierarchie-Sync im `SyncWorker`.

**Tech Stack:** ASP.NET Core 10, EF Core 10 (Service-DbContext), xUnit/FluentAssertions.

## Daten-Mapping (UAT-gated, S7-2/Sync-Regel-2-Warnung)

- `OrderNumber = HauptFA.ToString()`, `SubOrderNumber = SubFA.ToString()`, `ParentSubOrderNumber = VaterFA?.ToString()`.
- Materialisiert werden **Nicht-Blatt-Knoten** (`SubFA != 0`). Die Wurzel (`VaterFA == null`) ist bei
  Sage-Standarddaten `HauptFA == SubFA` → `OrderNumber == SubOrderNumber` (= Hauptauftrag laut Spec).
  **Annahme, am ersten echten Datenlauf zu bestätigen:** die Parent-Kette bleibt intakt, weil
  `child.VaterFA == parent.SubFA` (Tree-Builder-Konvention). Trifft `HauptFA == root.SubFA` nicht zu,
  ist der Wurzel-Knoten kein Hauptauftrag — in UAT prüfen (Node-Dezimal `NULL→0` analog Etappe A).
- App-verwaltete Felder bei Update **nie** überschreiben: `IsDone`, `PickingStatus`, `BdeStatus`,
  `ProductionWorkplaceId`, `IsCancelled/CancelledAt/CancelledBy`, `ExtraInfo`. Nur Quell-abgeleitete
  Felder (`Quantity`, `ArticleNumber`, `Description1/2`, `ParentSubOrderNumber` **nur wenn kein
  Konflikt**) werden aktualisiert.
- **Melderegel (konservativ, S7-2b):** `MissingToNotify` = neu als fehlend markierte FAs **ohne**
  bekannten Erledigt-Status (`IsDone`). Eine Sammelmail pro Lauf, keine Einzel-Mail je FA.

## File Structure

**Neu:**
- `IDEALAKEWMSService/Services/FaMaterializationPlanner.cs` — reine Records + `Plan(...)`.
- `IDEALAKEWMSService/Services/FaMaterializationSyncService.cs` + `IFaMaterializationSyncService.cs` — Orchestrator + Result-Record.
- `IDEALAKEWMSService.Tests/Services/FaMaterializationPlannerTests.cs` — Planer-Unit-Tests.

**Geändert:**
- `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs` — Konstante `FaMaterialization`.
- `IDEALAKEWMSService/Workers/SyncWorker.cs` — gated-Aufruf nach dem FA-Hierarchie-Sync.
- `IDEALAKEWMSService/Program.cs` (bzw. DI-Setup) — Registrierung `IFaMaterializationSyncService`.
- `IdealAkeWms/Controllers/HierarchieUmstellungController.cs` — Index refresht den Anzeige-Zustand
  bei GET (cross-process-sichere Umsetzung von Freigabe-Antwort 3 „Refresh nach Materialisierung").
- `docs/TESTSZENARIEN.md` — TS-64.

---

### Task 1: Reiner Planer `FaMaterializationPlanner` (TDD)

**Files:**
- Create: `IDEALAKEWMSService/Services/FaMaterializationPlanner.cs`
- Test: `IDEALAKEWMSService.Tests/Services/FaMaterializationPlannerTests.cs`

- [ ] **Step 1: Failing tests**

```csharp
using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class FaMaterializationPlannerTests
{
    private static MaterializationSourceOrder Src(string sub, string order, string? parent = null)
        => new(sub, order, parent);
    private static MaterializationExistingOrder Ex(string sub, string order, string? parent = null,
        bool missing = false, bool done = false) => new(sub, order, parent, missing, done);

    [Fact]
    public void LeereQuelle_WirdUebersprungen_NichtsMarkiert()
    {
        var plan = FaMaterializationPlanner.Plan(
            new MaterializationSourceOrder[] { },
            new[] { Ex("100", "100") });
        plan.Skipped.Should().BeTrue();
        plan.ToMarkMissing.Should().BeEmpty();
        plan.ToCreate.Should().BeEmpty();
    }

    [Fact]
    public void Regel1_NeuerSubFa_WirdAngelegt()
    {
        var plan = FaMaterializationPlanner.Plan(
            new[] { Src("100", "100"), Src("101", "100", "100") },
            new[] { Ex("100", "100") });
        plan.ToCreate.Should().BeEquivalentTo(new[] { "101" });
    }

    [Fact]
    public void Regel2_Verschwunden_WirdMarkiert_NieGeloescht_UndGemeldet()
    {
        var plan = FaMaterializationPlanner.Plan(
            new[] { Src("100", "100") },
            new[] { Ex("100", "100"), Ex("101", "100", "100") });
        plan.ToMarkMissing.Should().BeEquivalentTo(new[] { "101" });
        plan.MissingToNotify.Should().BeEquivalentTo(new[] { "101" });
    }

    [Fact]
    public void Regel2_VerschwundenAberErledigt_MarkiertAberNichtGemeldet()
    {
        var plan = FaMaterializationPlanner.Plan(
            new[] { Src("100", "100") },
            new[] { Ex("101", "100", "100", missing: false, done: true) });
        plan.ToMarkMissing.Should().Contain("101");
        plan.MissingToNotify.Should().BeEmpty();
    }

    [Fact]
    public void Regel2_BereitsMarkiert_WirdNichtErneutMarkiert()
    {
        var plan = FaMaterializationPlanner.Plan(
            new[] { Src("100", "100") },
            new[] { Ex("101", "100", "100", missing: true) });
        plan.ToMarkMissing.Should().BeEmpty();
        plan.MissingToNotify.Should().BeEmpty();
    }

    [Fact]
    public void Regel2_WiederAufgetaucht_WirdEntmarkt_Selbstheilend()
    {
        var plan = FaMaterializationPlanner.Plan(
            new[] { Src("100", "100"), Src("101", "100", "100") },
            new[] { Ex("101", "100", "100", missing: true) });
        plan.ToClearMissing.Should().BeEquivalentTo(new[] { "101" });
        plan.ToMarkMissing.Should().BeEmpty();
    }

    [Fact]
    public void Regel3_Umhaengung_WirdGemeldet_NichtUebernommen()
    {
        var plan = FaMaterializationPlanner.Plan(
            new[] { Src("101", "100", "200") },              // Quelle: neuer Vater 200
            new[] { Ex("101", "100", "100") });              // WMS: alter Vater 100
        plan.ReparentConflicts.Should().ContainSingle(c =>
            c.SubOrderNumber == "101" && c.OldParent == "100" && c.NewParent == "200");
    }
}
```

- [ ] **Step 2: Test rot**

Run: `dotnet test IDEALAKEWMSService.Tests --filter FaMaterializationPlannerTests`
Expected: FAIL (Typen fehlen).

- [ ] **Step 3: Planer implementieren**

```csharp
using System.Collections.Generic;
using System.Linq;

namespace IDEALAKEWMSService.Services;

/// <summary>Quell-FA (aus der lokalen FA-Struktur, Teil 1) — rein wertbasiert.</summary>
public sealed record MaterializationSourceOrder(
    string SubOrderNumber, string OrderNumber, string? ParentSubOrderNumber);

/// <summary>Zustand einer bereits materialisierten WMS-FA — rein wertbasiert.</summary>
public sealed record MaterializationExistingOrder(
    string SubOrderNumber, string OrderNumber, string? ParentSubOrderNumber,
    bool IsMissingMarked, bool IsKnownDone);

/// <summary>Erkannte Umhängung: Vater in der Quelle weicht vom gespeicherten Wert ab.</summary>
public sealed record ReparentConflict(string SubOrderNumber, string? OldParent, string? NewParent);

/// <summary>
/// Ergebnis der Materialisierung. Alle Listen sind <see cref="MaterializationExistingOrder.SubOrderNumber"/>
/// bzw. Quell-SubOrderNumbers. Bei <see cref="Skipped"/>=true wird NICHTS geschrieben.
/// </summary>
public sealed record MaterializationPlan(
    IReadOnlyList<string> ToCreate,
    IReadOnlyList<string> ToClearMissing,
    IReadOnlyList<string> ToMarkMissing,
    IReadOnlyList<ReparentConflict> ReparentConflicts,
    IReadOnlyList<string> MissingToNotify,
    bool Skipped,
    string? SkipReason);

/// <summary>
/// Reine, unit-testbare Sync-Regeln der Struktur→ProductionOrders-Materialisierung (Muster
/// <see cref="ProductionOrderReconciler"/>, keine DB). Drei Regeln: (1) neuer Sub-FA → anlegen;
/// (2) verschwundener FA → NICHT löschen, <c>SageMissingSince</c> setzen (selbstheilend beim
/// Wiederauftauchen); (3) Umhängung → melden, nicht still übernehmen. Empty-Source-Guard schützt
/// vor Massen-Markierung bei leerem Struktur-Read.
/// </summary>
public static class FaMaterializationPlanner
{
    public static MaterializationPlan Plan(
        IReadOnlyCollection<MaterializationSourceOrder> source,
        IReadOnlyCollection<MaterializationExistingOrder> existing)
    {
        var empty = System.Array.Empty<string>();
        if (source.Count == 0)
            return new MaterializationPlan(empty, empty, empty,
                System.Array.Empty<ReparentConflict>(), empty,
                Skipped: true, SkipReason: "Struktur-Quelle leer");

        // Dedup je SubOrderNumber (erste Zeile gewinnt; Quelle sollte eindeutig sein).
        var srcByKey = new Dictionary<string, MaterializationSourceOrder>(System.StringComparer.Ordinal);
        foreach (var s in source)
            if (!string.IsNullOrWhiteSpace(s.SubOrderNumber))
                srcByKey.TryAdd(s.SubOrderNumber, s);

        var existByKey = new Dictionary<string, MaterializationExistingOrder>(System.StringComparer.Ordinal);
        foreach (var e in existing)
            existByKey.TryAdd(e.SubOrderNumber, e);

        var toCreate = srcByKey.Keys.Where(k => !existByKey.ContainsKey(k)).ToList();

        var toClearMissing = new List<string>();
        var toMarkMissing = new List<string>();
        var reparent = new List<ReparentConflict>();
        var missingToNotify = new List<string>();

        foreach (var e in existByKey.Values)
        {
            if (srcByKey.TryGetValue(e.SubOrderNumber, out var s))
            {
                if (e.IsMissingMarked)
                    toClearMissing.Add(e.SubOrderNumber); // Regel 2: wieder aufgetaucht
                if (!ParentEquals(e.ParentSubOrderNumber, s.ParentSubOrderNumber))
                    reparent.Add(new ReparentConflict(e.SubOrderNumber,
                        Norm(e.ParentSubOrderNumber), Norm(s.ParentSubOrderNumber))); // Regel 3
            }
            else
            {
                if (!e.IsMissingMarked)
                {
                    toMarkMissing.Add(e.SubOrderNumber); // Regel 2: verschwunden
                    if (!e.IsKnownDone)
                        missingToNotify.Add(e.SubOrderNumber);
                }
            }
        }

        return new MaterializationPlan(toCreate, toClearMissing, toMarkMissing,
            reparent, missingToNotify, Skipped: false, SkipReason: null);
    }

    private static string? Norm(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static bool ParentEquals(string? a, string? b) =>
        string.Equals(Norm(a), Norm(b), System.StringComparison.Ordinal);
}
```

- [ ] **Step 4: Test grün**

Run: `dotnet test IDEALAKEWMSService.Tests --filter FaMaterializationPlannerTests`
Expected: PASS (7 Tests).

- [ ] **Step 5: Commit**

```bash
git add IDEALAKEWMSService/Services/FaMaterializationPlanner.cs IDEALAKEWMSService.Tests/Services/FaMaterializationPlannerTests.cs
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe C (1/5) - reiner FaMaterializationPlanner (3 Sync-Regeln, empty-source-Guard)"
```

---

### Task 2: SyncLog-Service-Konstante

**Files:**
- Modify: `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs`

- [ ] **Step 1: Konstante + All-Array**

```csharp
    public const string FaMaterialization = "FaMaterialization"; // Struktur -> ProductionOrders (Teil 7 Etappe C)
```
im `All`-Array nach `HierarchieUmstellung`:
```csharp
        SageLagerbuchung, FaHierarchy, HierarchieUmstellung, FaMaterialization,
```

- [ ] **Step 2: Build + Drift-Guard**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj && dotnet test IdealAkeWms.Tests --filter SyncLog`
Expected: 0 Fehler; SyncLog-Tests grün.

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/Services/SyncLogger/SyncLogServices.cs
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe C (2/5) - SyncLog-Service FaMaterialization"
```

---

### Task 3: Orchestrator `FaMaterializationSyncService`

**Files:**
- Create: `IDEALAKEWMSService/Services/IFaMaterializationSyncService.cs`
- Create: `IDEALAKEWMSService/Services/FaMaterializationSyncService.cs`

- [ ] **Step 1: Interface + Result-Record**

```csharp
namespace IDEALAKEWMSService.Services;

public sealed record FaMaterializationResult(
    int Created, int Marked, int Cleared, int ReparentConflicts, bool Skipped, bool DryRun);

public interface IFaMaterializationSyncService
{
    Task<FaMaterializationResult> RunAsync(bool dryRun, CancellationToken ct = default);
}
```

- [ ] **Step 2: Orchestrator** (liest lokale `FaHierarchyNodes`, mappt, wendet Plan an, Sammelmail, Log)

```csharp
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Materialisiert die lokale FA-Struktur (<see cref="FaHierarchyNode"/>, Teil 1) nach
/// <see cref="ProductionOrder"/>. Die Struktur ist die Quelle, ProductionOrders ist abgeleitet
/// (Transformation, kein zweiter Import). Läuft NUR bei <c>ProduktionsauftragHierarchisch=true</c>;
/// bei false bleibt der bestehende SageImportService-Pfad (AKE) unangetastet. Reine Entscheidung
/// im <see cref="FaMaterializationPlanner"/>, dieser Service ist die DB-/Mail-/Log-Schale.
/// </summary>
public class FaMaterializationSyncService : IFaMaterializationSyncService
{
    private const string ServiceUser = "IDEALAKEWMSService";

    private readonly ApplicationDbContext _ctx;
    private readonly IConfiguration _config;
    private readonly IMailService _mail;
    private readonly ILogger<FaMaterializationSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public FaMaterializationSyncService(
        ApplicationDbContext ctx,
        IConfiguration config,
        IMailService mail,
        ILogger<FaMaterializationSyncService> logger,
        ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _config = config;
        _mail = mail;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<FaMaterializationResult> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.FaMaterialization, ct);
        try
        {
            // Quelle: lokale FA-Struktur, Nicht-Blatt-Knoten (SubFA != 0). Mapping int->string.
            var nodes = await _ctx.FaHierarchyNodes.AsNoTracking()
                .Where(n => n.SubFA != 0)
                .ToListAsync(ct);

            var source = nodes
                .Select(n => new MaterializationSourceOrder(
                    n.SubFA.ToString(),
                    n.HauptFA.ToString(),
                    n.VaterFA.HasValue ? n.VaterFA.Value.ToString() : null))
                .ToList();

            var orders = await _ctx.ProductionOrders
                .Include(o => o.PickingStatus)
                .ToListAsync(ct);

            var existing = orders
                .Select(o => new MaterializationExistingOrder(
                    o.SubOrderNumber, o.OrderNumber, o.ParentSubOrderNumber,
                    IsMissingMarked: o.SageMissingSince.HasValue,
                    IsKnownDone: o.IsDone || (o.PickingStatus?.IsDonePicking ?? false)))
                .ToList();

            var plan = FaMaterializationPlanner.Plan(source, existing);

            if (plan.Skipped)
            {
                _logger.LogWarning("FA-Materialisierung übersprungen: {Reason}", plan.SkipReason);
                await run.LogWarningAsync($"Übersprungen: {plan.SkipReason}", ct: ct);
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["angelegt"] = 0, ["vermisst_neu"] = 0, ["wieder_da"] = 0, ["umhaengung_konflikt"] = 0,
                }, messageSuffix: $"[übersprungen: {plan.SkipReason}]", ct: ct);
                return new FaMaterializationResult(0, 0, 0, 0, Skipped: true, dryRun);
            }

            // Umhängungen: nie still übernehmen — protokollieren + im Log (Regel 3).
            foreach (var c in plan.ReparentConflicts)
                await run.LogWarningAsync(
                    $"Umhängung erkannt (NICHT übernommen): Sub-FA {c.SubOrderNumber} Vater {c.OldParent ?? "—"} -> {c.NewParent ?? "—"} (zur Klärung)",
                    reference: c.SubOrderNumber, ct: ct);

            if (dryRun)
            {
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["angelegt"] = plan.ToCreate.Count,
                    ["vermisst_neu"] = plan.ToMarkMissing.Count,
                    ["wieder_da"] = plan.ToClearMissing.Count,
                    ["umhaengung_konflikt"] = plan.ReparentConflicts.Count,
                }, messageSuffix: "[DryRun]", ct: ct);
                return new FaMaterializationResult(plan.ToCreate.Count, plan.ToMarkMissing.Count,
                    plan.ToClearMissing.Count, plan.ReparentConflicts.Count, Skipped: false, DryRun: true);
            }

            var now = DateTime.Now;
            var srcByKey = source.ToDictionary(s => s.SubOrderNumber, StringComparer.Ordinal);
            var nodeByKey = nodes.ToDictionary(n => n.SubFA.ToString(), n => n, StringComparer.Ordinal);
            var conflictKeys = plan.ReparentConflicts.Select(c => c.SubOrderNumber).ToHashSet(StringComparer.Ordinal);
            var toClear = plan.ToClearMissing.ToHashSet(StringComparer.Ordinal);
            var toMark = plan.ToMarkMissing.ToHashSet(StringComparer.Ordinal);
            var toCreate = plan.ToCreate.ToHashSet(StringComparer.Ordinal);

            // (1) Neu anlegen.
            foreach (var key in plan.ToCreate)
            {
                var n = nodeByKey[key];
                _ctx.ProductionOrders.Add(new ProductionOrder
                {
                    OrderNumber = n.HauptFA.ToString(),
                    SubOrderNumber = n.SubFA.ToString(),
                    ParentSubOrderNumber = n.VaterFA?.ToString(),
                    Quantity = n.Sollmenge,
                    ArticleNumber = n.Artnr,
                    Description1 = n.Bezeichnung1,
                    Description2 = n.Bezeichnung2,
                    IsDone = false,
                    SageMissingSince = null,
                    CreatedAt = now, CreatedBy = ServiceUser, CreatedByWindows = ServiceUser,
                });
            }

            // (2)/(3) Bestehende: Felder aktualisieren (App-Felder erhalten), missing setzen/klären.
            foreach (var o in orders)
            {
                var touched = false;
                if (toMark.Contains(o.SubOrderNumber)) { o.SageMissingSince = now; touched = true; }
                else if (toClear.Contains(o.SubOrderNumber)) { o.SageMissingSince = null; touched = true; }

                if (srcByKey.ContainsKey(o.SubOrderNumber) && nodeByKey.TryGetValue(o.SubOrderNumber, out var sn))
                {
                    // Quell-abgeleitete Felder aktualisieren; ParentSubOrderNumber NUR ohne Konflikt.
                    o.Quantity = sn.Sollmenge;
                    o.ArticleNumber = sn.Artnr;
                    o.Description1 = sn.Bezeichnung1;
                    o.Description2 = sn.Bezeichnung2;
                    if (!conflictKeys.Contains(o.SubOrderNumber))
                        o.ParentSubOrderNumber = sn.VaterFA?.ToString();
                    touched = true;
                }
                if (touched)
                {
                    o.ModifiedAt = now; o.ModifiedBy = ServiceUser; o.ModifiedByWindows = ServiceUser;
                }
            }

            await _ctx.SaveChangesAsync(ct);

            // Sammelmail (EINE pro Lauf) für neu-vermisste, nicht-erledigte FAs.
            await SendMissingDigestAsync(plan.MissingToNotify, ct);

            _logger.LogInformation(
                "FA-Materialisierung: {Created} neu, {Marked} vermisst, {Cleared} wieder da, {Conf} Umhäng-Konflikte.",
                plan.ToCreate.Count, plan.ToMarkMissing.Count, plan.ToClearMissing.Count, plan.ReparentConflicts.Count);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["angelegt"] = plan.ToCreate.Count,
                ["vermisst_neu"] = plan.ToMarkMissing.Count,
                ["wieder_da"] = plan.ToClearMissing.Count,
                ["umhaengung_konflikt"] = plan.ReparentConflicts.Count,
            }, ct: ct);

            return new FaMaterializationResult(plan.ToCreate.Count, plan.ToMarkMissing.Count,
                plan.ToClearMissing.Count, plan.ReparentConflicts.Count, Skipped: false, DryRun: false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "FA-Materialisierung fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw; // Fehlermail über RunResilientAsync im SyncWorker
        }
    }

    private async Task SendMissingDigestAsync(IReadOnlyList<string> missing, CancellationToken ct)
    {
        if (missing.Count == 0) return;
        if (!await ServiceSettings.GetBoolAsync(_config, "ErrorNotification:Enabled", false, ct)) return;
        var recipientsRaw = await ServiceSettings.GetValueAsync(_config, "ErrorNotification:Recipients", ct);
        var recipients = (recipientsRaw ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (recipients.Length == 0) return;

        var list = string.Join(", ", missing);
        var subject = $"FA-Materialisierung: {missing.Count} FA nicht mehr in Sage";
        var html = $"<p>Beim FA-Materialisierungs-Lauf sind folgende {missing.Count} Aufträge nicht mehr " +
                   $"in der Sage-Struktur (Status: vermisst, nicht gelöscht):</p><p>{list}</p>";
        var text = $"FA-Materialisierung: {missing.Count} FA nicht mehr in Sage:\n{list}";
        await _mail.SendAsync(subject, html, recipients, text, ct);
    }
}
```
> `ILagerbestandSyncService`-Nachbar zeigt das `_scopeFactory`/DbContext-Muster; hier reicht der per DI injizierte `ApplicationDbContext` (Scoped im SyncWorker-Scope). `IMailService.SendAsync`-Signatur verifiziert.

- [ ] **Step 3: Build**

Run: `dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj`
Expected: 0 Fehler.

- [ ] **Step 4: Commit**

```bash
git add IDEALAKEWMSService/Services/IFaMaterializationSyncService.cs IDEALAKEWMSService/Services/FaMaterializationSyncService.cs
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe C (3/5) - FaMaterializationSyncService (Orchestrator: EF-Apply, Sammelmail, Log)"
```

---

### Task 4: DI + SyncWorker-Verdrahtung + Anzeige-Refresh

**Files:**
- Modify: `IDEALAKEWMSService/Program.cs` (DI-Registrierung)
- Modify: `IDEALAKEWMSService/Workers/SyncWorker.cs`
- Modify: `IdealAkeWms/Controllers/HierarchieUmstellungController.cs`

- [ ] **Step 1: DI-Registrierung** (im Service-DI, bei den übrigen Sync-Services)

```csharp
services.AddScoped<IFaMaterializationSyncService, FaMaterializationSyncService>();
```
> Exakte Datei/Zeile via `grep -n "IFaHierarchySyncService" IDEALAKEWMSService/Program.cs` finden und daneben registrieren.

- [ ] **Step 2: SyncWorker — gated-Aufruf direkt NACH dem FA-Hierarchie-Sync-Block** (nach Zeile ~348)

```csharp
                // ---------------------------------------------------------------
                // FA-Materialisierung (IDEAL Teil 7): Struktur -> ProductionOrders.
                // NUR bei Master=true; läuft nach dem Struktur-Import (Quelle = lokale Nodes).
                // ---------------------------------------------------------------
                if (await ServiceSettings.GetBoolSafeAsync(_configuration, "ProduktionsauftragHierarchisch", false, stoppingToken))
                {
                    await RunResilientAsync("FA-Materialisierung", async () =>
                    {
                        using var matScope = _scopeFactory.CreateScope();
                        var matSync = matScope.ServiceProvider.GetRequiredService<IFaMaterializationSyncService>();
                        _logger.LogInformation("FA-Materialisierung startet (DryRun={DryRun})...", dryRun);
                        var r = await matSync.RunAsync(dryRun, stoppingToken);
                        _logger.LogInformation(
                            "FA-Materialisierung: {Created} neu, {Marked} vermisst, {Cleared} wieder da, {Conf} Konflikte, übersprungen={Skipped}.",
                            r.Created, r.Marked, r.Cleared, r.ReparentConflicts, r.Skipped);
                    }, stoppingToken);
                }
```

- [ ] **Step 3: Anzeige-Refresh cross-process-sicher** — `HierarchieUmstellungController.Index()` refresht den Cache bei GET (Freigabe-Antwort 3 „Refresh nach Materialisierung", da der Web-Cache nicht aus dem Service-Prozess erreichbar ist)

In `Index()` vor dem `GetValueAsync`:
```csharp
        await _status.RefreshAsync();
```
> Admin-only, niederfrequent — der Live-`AnyAsync` ist unkritisch und garantiert eine stets aktuelle Anzeige, unabhängig davon, in welchem Prozess der Sync lief.

- [ ] **Step 4: Build**

Run: `dotnet build IdealAkeWms.slnx`
Expected: 0 Fehler.

- [ ] **Step 5: Commit**

```bash
git add IDEALAKEWMSService/Program.cs IDEALAKEWMSService/Workers/SyncWorker.cs IdealAkeWms/Controllers/HierarchieUmstellungController.cs
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe C (4/5) - SyncWorker-Verdrahtung (gated) + DI + Anzeige-Refresh bei GET"
```

---

### Task 5: Gesamt-Build + Test + Testszenarien

**Files:**
- Modify: `docs/TESTSZENARIEN.md` (TS-64)

- [ ] **Step 1: Volltest**

Run: `dotnet build IdealAkeWms.slnx && dotnet test`
Expected: Web + Service grün, 0 Fehler.

- [ ] **Step 2: Testszenarien TS-64** — Regel 1 (neuer Sub-FA angelegt), Regel 2 (verschwunden → `SageMissingSince` statt Löschung, mit/ohne Rückmeldung; Wiederauftauchen → NULL; Sammelmail genau eine, nur unerwartete/nicht-erledigte), Regel 3 (Umhängung → Log-Eintrag, keine stille Übernahme), Empty-Source-Guard (leere Struktur → nichts markiert), Gating (Master aus → Sync läuft nicht; AKE unverändert), Mapping-UAT-Vorbehalt (Wurzel `HauptFA==SubFA`, Node-Dezimal `NULL→0`).

- [ ] **Step 3: Commit**

```bash
git add docs/TESTSZENARIEN.md
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe C - Testszenarien TS-64 (Materialisierung/3 Sync-Regeln)"
```

---

## Nach dem Code (Hauptcheckout — NICHT im Worktree)

- Etappen-Tabelle Teil 7 (Spec + Aufgaben-Notiz): Etappe C → erledigt + Commit-Range; Umsetzungsnotiz (Mapping-Annahme, Melderegel, Empty-Guard).
- `secondbrain/tests/testszenarien-index.md`: TS-64 + „besonderes Gewicht" (Mapping/Mail nur am echten Datenlauf pruefbar).
- Status Teil 7 bleibt `InUmsetzung` (D/E offen). Kein qa-agent, kein Merge, kein Push.

## Nicht in Etappe C (bewusst)

- Lookup-Härtung (`OrderNumber`-Sweep) + `GetAllByFaAndOperationAsync` + FA-Zusatzinfos-Fold-2-Sperre + hierarchische Reconcile/Fold-2-Tests → **Etappe D**.
- Version-Bump/Changelog/Brain-Dauerwissen → **Etappe E**.
- Reale Sage-Daten-Validierung des Mappings + Schärfung der Melderegel (behält die IDEAL-View fertige Aufträge?) → **erster echter Datenlauf / Schranke 2**.
