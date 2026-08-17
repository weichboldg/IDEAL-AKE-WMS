# IDEAL Teil 7 — Etappe D: Lookup-Härtung + Auto-Erledigt-Sperre — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Die `OrderNumber`-Mehrdeutigkeit im hierarchischen Modus härten: mengenwertige Lookup-Naht (`GetAllByFaAndOperationAsync`) + Logging im Einzel-Lookup (AK 10), dreistufige Auto-Erledigt-Sperre in `FaZusatzinfoSyncService` (AK 11), gezielte hierarchische Unit-Tests für die zwei riskantesten UPDATE-Pfade (AK 12) und der schriftliche `OrderNumber`-Sweep (AK 8).

**Architecture:** Teil 7 ändert NUR Schema-/Lookup-Semantik im Server-Code — die Umstellung der Aufrufer auf die mengenwertige Variante ist Teil 8. Der Einzel-Lookup bleibt verhaltensgleich (First), protokolliert aber Mehrfachtreffer. Die Auto-Erledigt-Sperre ist dreistufig: (1) datengetrieben (Fold 2 skip bei WaNummer-Match > 1), (2) Schalter (`Sync:FaZusatzinfoAutoErledigtEnabled`, vom Etappe-B-Guard bei hierarchischen Daten gegen Einschalten gesperrt, Master-Flip schaltet ab), (3) Test.

**Tech Stack:** EF Core 10 (InMemory-Tests), xUnit/FluentAssertions/Moq.

## Sweep-Grundsatz (AK 8)

Der `OrderNumber`-Sweep ist ein **schriftliches Review-Artefakt** (in der Aufgaben-Notiz), kein Massen-Refactor: jede Fundstelle bekommt ein Urteil „unkritisch (Gruppen-Lookup/Anzeige, bleibt `OrderNumber`)" oder „kritisch (Eindeutigkeits-Lookup → mengenwertige Naht, Aufrufer-Umstellung = Teil 8)". Code-Änderung in Teil 7 nur dort, wo eine **Naht** gebraucht wird (AK 10, `GetByFaAndOperationAsync`). 14 ist Untergrenze.

---

### Task 1: AK 10 — mengenwertige Lookup-Naht + Logging im Einzel-Lookup

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/WorkOperationRepository.cs`
- Test: `IdealAkeWms.Tests/Repositories/WorkOperationRepositoryTests.cs` (neu oder vorhanden)

- [ ] **Step 1: Interface erweitern**

```csharp
    /// <summary>
    /// Mengenwertige Variante (Teil 7 Etappe D, AK 10): liefert ALLE Arbeitsgänge zu FA+Operation.
    /// Im hierarchischen Modus ist <c>OrderNumber</c> nicht mehr eindeutig — der Einzel-Lookup
    /// <see cref="GetByFaAndOperationAsync"/> kann dann mehrdeutig sein. Die Umstellung der Aufrufer
    /// auf diese Variante ist Teil 8; Teil 7 liefert nur die Naht + protokolliert Mehrfachtreffer.
    /// </summary>
    Task<List<WorkOperation>> GetAllByFaAndOperationAsync(string faNumber, string operationNumber);
```

- [ ] **Step 2: Impl + ILogger im Ctor + Multi-Hit-Log im Einzel-Lookup (Verhalten unverändert)**

Ctor um `ILogger<WorkOperationRepository>` ergänzen; `GetByFaAndOperationAsync` fetcht bis zu 2 Zeilen, loggt bei ≥2 eine Warnung, gibt **weiter die erste** zurück (kein Verhaltenswechsel):
```csharp
    private readonly ILogger<WorkOperationRepository> _logger;

    public WorkOperationRepository(ApplicationDbContext context, ILogger<WorkOperationRepository> logger)
        : base(context) { _logger = logger; }

    public async Task<WorkOperation?> GetByFaAndOperationAsync(string faNumber, string operationNumber)
    {
        // Verhalten unverändert (First), aber Mehrfachtreffer protokollieren (AK 10, hierarchischer Modus).
        var hits = await _dbSet.Include(w => w.ProductionOrder)
            .Where(w => w.ProductionOrder.OrderNumber == faNumber && w.OperationNumber == operationNumber)
            .Take(2).ToListAsync();
        if (hits.Count > 1)
            _logger.LogWarning(
                "GetByFaAndOperationAsync: Mehrfachtreffer für FA {Fa} / Operation {Op} (hierarchischer Modus) — Einzel-Lookup liefert die erste Zeile; Aufrufer sollte GetAllByFaAndOperationAsync nutzen (Teil 8).",
                faNumber, operationNumber);
        return hits.FirstOrDefault();
    }

    public Task<List<WorkOperation>> GetAllByFaAndOperationAsync(string faNumber, string operationNumber) =>
        _dbSet.Include(w => w.ProductionOrder)
            .Where(w => w.ProductionOrder.OrderNumber == faNumber && w.OperationNumber == operationNumber)
            .ToListAsync();
```
> `using Microsoft.Extensions.Logging;` ergänzen. Die alte Expression-Body-Fassung von `GetByFaAndOperationAsync` ersetzen.

- [ ] **Step 3: Test** (mengenwertig liefert beide; Einzel-Lookup liefert eine von zwei)

```csharp
[Fact]
public async Task GetAllByFaAndOperation_ReturnsAllSubFaRows_WhileSingleReturnsOne()
{
    using var ctx = TestDbContextFactory.Create();
    var haupt = new ProductionOrder { OrderNumber = "100", SubOrderNumber = "100",
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
    var sub = new ProductionOrder { OrderNumber = "100", SubOrderNumber = "101", ParentSubOrderNumber = "100",
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
    ctx.ProductionOrders.AddRange(haupt, sub);
    ctx.SaveChanges();
    ctx.WorkOperations.AddRange(
        new WorkOperation { ProductionOrderId = haupt.Id, OperationNumber = "BG" },
        new WorkOperation { ProductionOrderId = sub.Id,   OperationNumber = "BG" });
    ctx.SaveChanges();

    var repo = new WorkOperationRepository(ctx, NullLogger<WorkOperationRepository>.Instance);

    (await repo.GetAllByFaAndOperationAsync("100", "BG")).Should().HaveCount(2);
    (await repo.GetByFaAndOperationAsync("100", "BG")).Should().NotBeNull(); // erste Zeile, kein Crash
}
```
> `WorkOperation`-Pflichtfelder aus dem Modell prüfen (ggf. weitere setzen). `NullLogger` via `using Microsoft.Extensions.Logging.Abstractions;`.

- [ ] **Step 4: Build + Test**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj && dotnet test IdealAkeWms.Tests --filter WorkOperation`
Expected: 0 Fehler, grün.

- [ ] **Step 5: Commit** `epic … Teil 7 Etappe D (1/4) - GetAllByFaAndOperationAsync + Multi-Hit-Log im Einzel-Lookup (AK 10)`

---

### Task 2: AK 11 — dreistufige Auto-Erledigt-Sperre

**Files:**
- Modify: `IdealAkeWms/Models/ServiceSettingDefinitions.cs` (Katalog-Key)
- Modify: `IDEALAKEWMSService/Services/IFaZusatzinfoSyncService.cs` (Signatur)
- Modify: `IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs` (Stage 1 + 2)
- Modify: `IDEALAKEWMSService/Workers/SyncWorker.cs` (Schalter durchreichen)
- Test: `IDEALAKEWMSService.Tests/Services/FaZusatzinfoSyncServiceTests.cs` (Stage 3)

- [ ] **Step 1: Katalog-Key** (Default `true` = AKE-Verhalten unverändert; vom Etappe-B-Guard geschützt)

In `ServiceSettingDefinitions.All` bei den FaZusatzinfo-Keys:
```csharp
        new("Sync:FaZusatzinfoAutoErledigtEnabled",  ServiceSettingType.Bool, "true",  "Sync", "Auto-Erledigt (Fold 2): FAs mit Sage-Status verpackt/abgeholt automatisch auf Komm-Erledigt setzen. Default an (AKE). Im hierarchischen IDEAL-Modus NICHT aktivierbar (HauptFA+Sub-FAs teilen die FA-Nummer); der Master-Flip schaltet ihn automatisch ab. Zusaetzliche datengetriebene Sperre: bei WA-Mehrfachtreffer wird Fold 2 uebersprungen."),
```

- [ ] **Step 2: Signatur** — neuer Trailing-Default-Param (bestehende 2-arg-Aufrufer unverändert)

`IFaZusatzinfoSyncService`:
```csharp
    Task<SyncResult> SyncAsync(bool dryRun, int autoDoneMaxPerRun, bool autoErledigtEnabled = true, CancellationToken ct = default);
```
`FaZusatzinfoSyncService.SyncAsync`-Signatur analog anpassen.

- [ ] **Step 3: Stage 1 (datengetrieben) + Stage 2 (Schalter)** in der Kandidaten-Sammlung

Im `foreach (var row in rows)`-Block, VOR dem `foreach (var order in matches)`: bei Mehrfachtreffer Fold 2 für diese WA überspringen + einmal loggen. Und die Kandidaten-Aufnahme zusätzlich an `autoErledigtEnabled` koppeln:
```csharp
                // Fold-2-Sperre (Teil 7 AK 11): im hierarchischen Modus teilen HauptFA + Sub-FAs
                // dieselbe OrderNumber -> matches.Count > 1. Ein "verpackt/abgeholt" am HauptFA darf
                // NICHT die ganze Gruppe schliessen. Datengetriebene Sperre (Stage 1) greift
                // unabhaengig vom Schalter (Stage 2).
                var fold2Allowed = autoErledigtEnabled && matches.Count == 1;
                if (autoErledigtEnabled && matches.Count > 1)
                    await run.LogWarningAsync(
                        $"Auto-Erledigt uebersprungen (WA {wa}): {matches.Count} FA-Treffer (hierarchisch) — Komm-Erledigt wuerde die ganze Gruppe treffen.",
                        reference: wa, ct: ct);
```
und im inneren `foreach (var order in matches)` die Kandidaten-Bedingung um `fold2Allowed` erweitern:
```csharp
                    if (fold2Allowed
                        && FaZusatzinfoStatus.IstVerpacktOderAbgeholt(row.Status)
                        && !order.IsDone
                        && !order.IsCancelled
                        && order.PickingStatus?.IsDonePicking != true
                        && !autoDoneCandidates.ContainsKey(order.Id))
                    {
                        autoDoneCandidates[order.Id] = (order, row.Status!.Trim());
                    }
```

- [ ] **Step 4: SyncWorker** — Schalter DB-first lesen + durchreichen

Im FA-Zusatzinfo-Block nach dem `autoDoneCap`-Read:
```csharp
                        var autoErledigt = await ServiceSettings.GetBoolSafeAsync(
                            _configuration, "Sync:FaZusatzinfoAutoErledigtEnabled", true, stoppingToken);
```
und `SyncAsync(dryRun, autoDoneCap, autoErledigt, stoppingToken)`.

- [ ] **Step 5: Stage 3 — hierarchischer Test** (zwei Sub-FAs derselben OrderNumber, verpackt → keine Änderung)

```csharp
// Teil 7 AK 11c: zwei Sub-FAs derselben OrderNumber — "abgeholt" am HauptFA darf den
// Komm-Erledigt-Status der Geschwister NICHT setzen (Fold-2-Sperre bei Mehrfachtreffer).
[Fact]
public async Task Fold2_ZweiSubFaSelbeOrderNumber_VerpacktAmHauptFa_SetztKeineErledigt()
{
    var (svc, reader, ctx, fakeLogger, _) = Build();
    var haupt = new ProductionOrder { OrderNumber = "100", SubOrderNumber = "100",
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
    var sub = new ProductionOrder { OrderNumber = "100", SubOrderNumber = "101", ParentSubOrderNumber = "100",
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
    ctx.ProductionOrders.AddRange(haupt, sub);
    ctx.SaveChanges();
    reader.Rows = new() { Row("100", status: "abgeholt") };

    await svc.SyncAsync(dryRun: false, autoDoneMaxPerRun: 100, autoErledigtEnabled: true);

    ctx.ProductionOrderPickingStatuses.Any(p => p.IsDonePicking).Should().BeFalse();
    fakeLogger.Runs[0].FinalCounts!["erledigt-gesetzt"].Should().Be(0);
}
```

- [ ] **Step 6: Build + Test**

Run: `dotnet build IdealAkeWms.slnx && dotnet test IDEALAKEWMSService.Tests --filter FaZusatzinfo && dotnet test IdealAkeWms.Tests --filter ServiceSetting`
Expected: 0 Fehler; grün (Drift-Guard akzeptiert neuen Key).

- [ ] **Step 7: Commit** `epic … Teil 7 Etappe D (2/4) - dreistufige Auto-Erledigt-Sperre (datengetrieben + Schalter + Test) (AK 11)`

---

### Task 3: AK 12 — hierarchischer Test Reconcile-UPDATE-Pfad

**Files:**
- Modify: `IDEALAKEWMSService.Tests/Services/ProductionOrderReconcilerTests.cs`

- [ ] **Step 1: Test** — Gruppen-Semantik bei doppelter OrderNumber

```csharp
// Teil 7 AK 12: Der Reconcile-Planer arbeitet auf OrderNumber (Gruppen-Schluessel). Im
// hierarchischen Modus teilen HauptFA + Sub-FAs dieselbe OrderNumber; verschwindet die HauptFA
// aus Sage, wird die GANZE Gruppe storniert (bewusst), taucht sie auf, keine Stornierung.
[Fact]
public void Plan_HierarchischeGruppe_SelbeOrderNumber_WirdAlsGruppeBehandelt()
{
    var wms = new[]
    {
        new WmsOrderState("100", IsDone: false, IsCancelled: false), // HauptFA
        new WmsOrderState("100", IsDone: false, IsCancelled: false), // Sub-FA (selbe OrderNumber)
    };
    // HauptFA noch in Sage -> nichts stornieren.
    var keep = ProductionOrderReconciler.Plan(new[] { "100" }, wms, maxCancelPerRun: 100);
    keep.ToCancel.Should().BeEmpty();

    // HauptFA weg -> beide (Gruppe) als Storno-Kandidat.
    var gone = ProductionOrderReconciler.Plan(new[] { "999" }, wms, maxCancelPerRun: 100);
    gone.ToCancel.Should().OnlyContain(x => x == "100");
    gone.ToCancel.Should().HaveCount(2);
}
```
> Signatur `WmsOrderState`/`Plan` aus `ProductionOrderReconciler.cs` verifiziert (`WmsOrderState(string, bool, bool)`).

- [ ] **Step 2: Test**

Run: `dotnet test IDEALAKEWMSService.Tests --filter ProductionOrderReconciler`
Expected: grün.

- [ ] **Step 3: Commit** `epic … Teil 7 Etappe D (3/4) - hierarchischer Reconcile-Gruppen-Test (AK 12)`

---

### Task 4: AK 8 — OrderNumber-Sweep dokumentieren + Testszenarien + Volltest

**Files:**
- (Hauptcheckout) `secondbrain/aufgaben/2026-07-29-standort-ideal-teil-7.md` — Sweep-Katalog + Urteile.
- Modify (Worktree): `docs/TESTSZENARIEN.md` — TS-65.

- [ ] **Step 1: Sweep-Katalog** (aus dem Explore-Subagenten) als Tabelle in die Aufgaben-Notiz (Hauptcheckout): je Fundstelle Datei:Zeile | Semantik | Urteil. Kritische Stellen ohne Naht-Bedarf explizit als „Aufrufer-Umstellung Teil 8" markieren.

- [ ] **Step 2: Testszenarien TS-65** — mengenwertiger Lookup (Log bei Mehrfachtreffer, Verhalten unverändert), Auto-Erledigt-Sperre dreistufig (datengetrieben/Schalter/Geschwister-Test), Reconcile-Gruppen-Semantik.

- [ ] **Step 3: Volltest** `dotnet build IdealAkeWms.slnx && dotnet test` → Web + Service grün.

- [ ] **Step 4: Commit** `epic … Teil 7 Etappe D - Testszenarien TS-65 (Lookup-Härtung/Auto-Erledigt-Sperre)`

---

## Nach dem Code (Hauptcheckout)

- Etappen-Tabelle Teil 7 (Spec + Aufgaben): D → erledigt + Commit-Range; Sweep-Katalog; Umsetzungsnotiz.
- `secondbrain/tests/testszenarien-index.md`: TS-65.
- Status Teil 7 bleibt `InUmsetzung` (nur E offen). Kein qa-agent, kein Merge, kein Push.

## Nicht in Etappe D (bewusst)

- Umstellung der Aufrufer von Einzel- auf mengenwertigen Lookup → **Teil 8**.
- Version-Bump/Changelog/Brain-Dauerwissen → **Etappe E**.
