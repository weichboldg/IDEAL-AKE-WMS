# IDEAL Teil 7 — Etappe B: Guard + Einwegtor + Runbook + Audit — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Den Master-Schalter `ProduktionsauftragHierarchisch` als **Einwegtor** über eine dedizierte Umschalt-Seite bauen, mit einem Domänen-Wächter (`HierarchischeStrukturGuard`) als einzigem Choke-Point über alle ServiceSettings-Schreibwege, Audit-Protokollierung und Rückbau-Runbook.

**Architecture:** Reiner, unit-getesteter Planer (`HierarchischeStrukturGuard.Evaluate`, Muster `ProductionOrderReconciler`) entscheidet Erlaubt/Abgelehnt aus (Key, Ist-Wert, Soll-Wert, `hierarchicalDataExists`). Die Durchsetzung sitzt strukturell als **Decorator** auf `IServiceSettingRepository.UpsertAsync/DeleteAsync` (die einzige Datenzugriffs-Naht, durch die alle vier Controller-Schreibwege laufen) — kein Caller kann den Guard umgehen. Die dedizierte `HierarchieUmstellungController`-Seite ist der einzige sanktionierte UI-Schreibweg; die generische ServiceSettings-Maske zeigt den Master nur read-only mit Link. Anzeige-Zustand ist gecacht (`HierarchischeStrukturStatus`), Schreibpfad prüft live (Freigabe-Antwort 3). Audit via `ISyncLogger` (Service `HierarchieUmstellung`).

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10, Bootstrap 5, xUnit/FluentAssertions.

## Zuschnitt-Entscheidung (Schalter-Inventur, S7-8)

Die „abhängigen Schalter" sind **genau zwei**: der **Master** `ProduktionsauftragHierarchisch` und der **Auto-Erledigt-Schalter**. Ein dritter Gate-Schalter ist nicht nötig — die übrigen `OrderNumber`-abhängigen Codepfade werden in **Etappe D** gehärtet (Lookup-Sweep), nicht per Schalter gesperrt.

Etappe B legt **nur den Master-Key** an (Katalog + Seed + read-only-UI + Umschalt-Seite). Der **Auto-Erledigt-Key** (`Sync:FaZusatzinfoAutoErledigtEnabled`) wird vom Guard bereits **benannt und geschützt**, seine Katalog-Eintragung + die Fold-2-Verdrahtung in `FaZusatzinfoSyncService` bleiben **Etappe D** (dort bekommt der Schlüssel seinen Leser — kein leserloser Katalog-Key in B). Der Controller entschärft Auto-Erledigt beim Master-Flip bereits defensiv (No-op, solange der Key nicht existiert).

## File Structure

**Neu:**
- `IdealAkeWms/Services/HierarchischeStruktur/HierarchischeStrukturKeys.cs` — Key-Konstanten + `IsGuarded`/`Map`.
- `IdealAkeWms/Services/HierarchischeStruktur/HierarchischeStrukturGuard.cs` — reiner Planer (`Evaluate`) + `GuardedSetting`, `GuardDecision`, `HierarchischeStrukturGuardException`.
- `IdealAkeWms/Services/HierarchischeStruktur/HierarchischeStrukturStatus.cs` — Singleton, gecachter Anzeige-Zustand + `RefreshAsync`.
- `IdealAkeWms/Data/Repositories/GuardedServiceSettingRepository.cs` — Decorator (Durchsetzung).
- `IdealAkeWms/Controllers/HierarchieUmstellungController.cs` — Umschalt-Seite (GET Index, POST Enable/Disable).
- `IdealAkeWms/Models/ViewModels/HierarchieUmstellungViewModel.cs` — VM.
- `IdealAkeWms/Views/HierarchieUmstellung/Index.cshtml` — Seite.
- `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md` — Rückbau-Runbook.
- `IdealAkeWms.Tests/Services/HierarchischeStrukturGuardTests.cs` — Planer-Unit-Tests.
- `IdealAkeWms.Tests/Data/GuardedServiceSettingRepositoryTests.cs` — Decorator-Durchsetzungs-Tests (Moq).

**Geändert:**
- `IdealAkeWms/Models/ServiceSettingDefinitions.cs` — Master-Key im Katalog.
- `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs` — Konstante `HierarchieUmstellung`.
- `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs` + `ProductionOrderRepository.cs` — `HierarchicalDataExistsAsync()`.
- `IdealAkeWms/Controllers/ServiceSettingsController.cs` — `SaveSettings` fängt Guard-Ablehnung, protokolliert.
- `IdealAkeWms/Views/ServiceSettings/Index.cshtml` — Master read-only + Link.
- `IdealAkeWms/Program.cs` — Decorator-Registrierung, Status-Singleton + Startup-Refresh.
- `README.md` — Runbook-Verlinkung.

---

### Task 1: Key-Konstanten + reiner Guard-Planer (TDD)

**Files:**
- Create: `IdealAkeWms/Services/HierarchischeStruktur/HierarchischeStrukturKeys.cs`
- Create: `IdealAkeWms/Services/HierarchischeStruktur/HierarchischeStrukturGuard.cs`
- Test: `IdealAkeWms.Tests/Services/HierarchischeStrukturGuardTests.cs`

- [ ] **Step 1: Key-Konstanten anlegen**

`HierarchischeStrukturKeys.cs`:
```csharp
namespace IdealAkeWms.Services.HierarchischeStruktur;

/// <summary>
/// Die vom <see cref="HierarchischeStrukturGuard"/> geschützten ServiceSettings-Keys
/// (Master-Einwegtor + Auto-Erledigt). Der Auto-Erledigt-Key wird hier bereits benannt
/// und geschützt; sein Katalog-Eintrag + die Fold-2-Verdrahtung folgen in Etappe D.
/// </summary>
public static class HierarchischeStrukturKeys
{
    public const string Master = "ProduktionsauftragHierarchisch";
    public const string AutoErledigt = "Sync:FaZusatzinfoAutoErledigtEnabled";

    public static bool IsGuarded(string key) =>
        string.Equals(key, Master, StringComparison.Ordinal)
        || string.Equals(key, AutoErledigt, StringComparison.Ordinal);

    public static GuardedSetting Map(string key) =>
        string.Equals(key, Master, StringComparison.Ordinal)
            ? GuardedSetting.Master
            : GuardedSetting.AutoErledigt;
}
```

- [ ] **Step 2: Failing test schreiben**

`HierarchischeStrukturGuardTests.cs`:
```csharp
using FluentAssertions;
using IdealAkeWms.Services.HierarchischeStruktur;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class HierarchischeStrukturGuardTests
{
    // Kein Datensatz -> alles frei (Einwegtor noch nicht geschlossen).
    [Theory]
    [InlineData(GuardedSetting.Master, false, true)]
    [InlineData(GuardedSetting.Master, true, false)]
    [InlineData(GuardedSetting.AutoErledigt, false, true)]
    public void OhneHierarchischeDaten_JedeAenderungErlaubt(GuardedSetting s, bool cur, bool req)
    {
        HierarchischeStrukturGuard.Evaluate(s, cur, req, hierarchicalDataExists: false)
            .Allowed.Should().BeTrue();
    }

    // No-op-Schreibvorgang (Ist == Soll) ist immer erlaubt, auch gesperrt.
    [Fact]
    public void GleicherWert_ImmerErlaubt_AuchGesperrt()
    {
        HierarchischeStrukturGuard.Evaluate(GuardedSetting.Master, true, true, true)
            .Allowed.Should().BeTrue();
    }

    // Gesperrt: Master lässt sich nicht mehr deaktivieren, Runbook referenziert.
    [Fact]
    public void MasterDeaktivieren_Gesperrt_Abgelehnt_MitRunbook()
    {
        var d = HierarchischeStrukturGuard.Evaluate(GuardedSetting.Master, true, false, true);
        d.Allowed.Should().BeFalse();
        d.RejectionReason.Should().Contain("RUNBOOK-FA-HIERARCHIE-RUECKBAU.md");
    }

    // Gesperrt: Auto-Erledigt lässt sich nicht einschalten...
    [Fact]
    public void AutoErledigtEinschalten_Gesperrt_Abgelehnt()
    {
        HierarchischeStrukturGuard.Evaluate(GuardedSetting.AutoErledigt, false, true, true)
            .Allowed.Should().BeFalse();
    }

    // ...aber ausschalten bleibt erlaubt (Master-Flip schaltet ab).
    [Fact]
    public void AutoErledigtAusschalten_Gesperrt_Erlaubt()
    {
        HierarchischeStrukturGuard.Evaluate(GuardedSetting.AutoErledigt, true, false, true)
            .Allowed.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Test rot verifizieren**

Run: `dotnet test IdealAkeWms.Tests --filter HierarchischeStrukturGuardTests`
Expected: FAIL (Typen nicht vorhanden).

- [ ] **Step 4: Planer implementieren**

`HierarchischeStrukturGuard.cs`:
```csharp
namespace IdealAkeWms.Services.HierarchischeStruktur;

public enum GuardedSetting { Master, AutoErledigt }

/// <summary>Ergebnis der Wächter-Prüfung. Bei <see cref="Allowed"/>=false trägt
/// <see cref="RejectionReason"/> die anwenderlesbare Begründung.</summary>
public sealed record GuardDecision(bool Allowed, string? RejectionReason);

/// <summary>Wird geworfen, wenn ein Schreibversuch auf einen geschützten Key
/// vom Wächter abgelehnt wird (Choke-Point-Decorator).</summary>
public sealed class HierarchischeStrukturGuardException : Exception
{
    public HierarchischeStrukturGuardException(string message) : base(message) { }
}

/// <summary>
/// Reiner, unit-testbarer Wächter des FA-Hierarchie-Einwegtors (Muster
/// <c>ProductionOrderReconciler</c>, keine DB). Sperrbedingung ist datengetrieben:
/// <c>hierarchicalDataExists</c> = EXISTS(ProductionOrders WHERE OrderNumber &lt;&gt; SubOrderNumber).
/// </summary>
public static class HierarchischeStrukturGuard
{
    public const string MasterLockedReason =
        "Kann nicht mehr geändert werden: es liegen Aufträge mit Sub-FA-Struktur vor. " +
        "Rückbau ist nicht über die Anwendung umkehrbar — nur über das Runbook, " +
        "siehe docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md";

    public const string AutoErledigtLockedReason =
        "Auto-Erledigt kann nicht aktiviert werden, solange hierarchische Aufträge existieren: " +
        "HauptFA und Sub-FAs teilen dieselbe FA-Nummer, ein Komm-Erledigt am HauptFA träfe die ganze Gruppe.";

    public static GuardDecision Evaluate(
        GuardedSetting setting,
        bool currentValue,
        bool requestedValue,
        bool hierarchicalDataExists)
    {
        // No-op oder Tor noch offen -> frei.
        if (currentValue == requestedValue || !hierarchicalDataExists)
            return new GuardDecision(true, null);

        return setting switch
        {
            GuardedSetting.Master => new GuardDecision(false, MasterLockedReason),
            // Ausschalten immer erlaubt (Master-Flip schaltet ab); nur Einschalten sperren.
            GuardedSetting.AutoErledigt => requestedValue
                ? new GuardDecision(false, AutoErledigtLockedReason)
                : new GuardDecision(true, null),
            _ => new GuardDecision(true, null),
        };
    }
}
```

- [ ] **Step 5: Test grün verifizieren**

Run: `dotnet test IdealAkeWms.Tests --filter HierarchischeStrukturGuardTests`
Expected: PASS (6 Tests).

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Services/HierarchischeStruktur/ IdealAkeWms.Tests/Services/HierarchischeStrukturGuardTests.cs
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe B (1/8) - reiner HierarchischeStrukturGuard-Planer + Keys"
```

---

### Task 2: `HierarchicalDataExistsAsync()` im ProductionOrder-Repository

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs`

- [ ] **Step 1: Interface-Methode ergänzen**

In `IProductionOrderRepository.cs` (bei den übrigen Task-Signaturen):
```csharp
/// <summary>
/// Live-Prüfung der Einwegtor-Sperrbedingung: existiert mindestens eine Zeile mit
/// <c>OrderNumber != SubOrderNumber</c> (= hierarchische Materialisierung hat stattgefunden)?
/// Nur auf dem Schreibpfad des Wächters verwenden — für die Anzeige den gecachten
/// <c>HierarchischeStrukturStatus</c> nutzen (Freigabe-Antwort 3).
/// </summary>
Task<bool> HierarchicalDataExistsAsync();
```

- [ ] **Step 2: Implementierung**

In `ProductionOrderRepository.cs` (Kontext-Feld `_context`/`_db` prüfen und übernehmen):
```csharp
public async Task<bool> HierarchicalDataExistsAsync()
    => await _context.ProductionOrders
        .AsNoTracking()
        .AnyAsync(p => p.OrderNumber != p.SubOrderNumber);
```
> Prüfen: heißt das DbContext-Feld `_context` oder `_db`? Feldnamen aus der Datei übernehmen. `AnyAsync` braucht `using Microsoft.EntityFrameworkCore;` (i.d.R. vorhanden).

- [ ] **Step 3: Build**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: 0 Fehler.

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe B (2/8) - HierarchicalDataExistsAsync (Live-Sperrbedingung)"
```

---

### Task 3: Gecachter Anzeige-Zustand `HierarchischeStrukturStatus`

**Files:**
- Create: `IdealAkeWms/Services/HierarchischeStruktur/HierarchischeStrukturStatus.cs`

- [ ] **Step 1: Singleton mit Refresh-Naht**

```csharp
using IdealAkeWms.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace IdealAkeWms.Services.HierarchischeStruktur;

/// <summary>
/// Gecachter Anzeige-Zustand der Einwegtor-Sperrbedingung (Freigabe-Antwort 3):
/// die Anzeige liest hier gecacht, der Schreibpfad (<c>GuardedServiceSettingRepository</c>)
/// prüft live. Refresh am Start, nach jedem Master-Flip und (ab Etappe C) am Ende des
/// Materialisierungs-Syncs — bewusst kein Timer.
/// </summary>
public sealed class HierarchischeStrukturStatus
{
    private readonly IServiceScopeFactory _scopeFactory;
    private volatile bool _dataExists;

    public HierarchischeStrukturStatus(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public bool HierarchicalDataExists => _dataExists;
    public DateTimeOffset? LastRefreshedUtc { get; private set; }

    public async Task RefreshAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProductionOrderRepository>();
        _dataExists = await repo.HierarchicalDataExistsAsync();
        LastRefreshedUtc = DateTimeOffset.UtcNow;
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: 0 Fehler.

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/Services/HierarchischeStruktur/HierarchischeStrukturStatus.cs
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe B (3/8) - gecachter Anzeige-Zustand HierarchischeStrukturStatus"
```

---

### Task 4: Durchsetzungs-Decorator `GuardedServiceSettingRepository` (TDD)

**Files:**
- Create: `IdealAkeWms/Data/Repositories/GuardedServiceSettingRepository.cs`
- Test: `IdealAkeWms.Tests/Data/GuardedServiceSettingRepositoryTests.cs`

- [ ] **Step 1: Failing test (Moq)**

```csharp
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services.HierarchischeStruktur;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Data;

public class GuardedServiceSettingRepositoryTests
{
    private static (GuardedServiceSettingRepository repo, Mock<IServiceSettingRepository> inner,
                    Mock<IProductionOrderRepository> orders) Build(string currentMaster, bool dataExists)
    {
        var inner = new Mock<IServiceSettingRepository>();
        inner.Setup(r => r.GetValueAsync(HierarchischeStrukturKeys.Master))
             .ReturnsAsync(currentMaster);
        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(o => o.HierarchicalDataExistsAsync()).ReturnsAsync(dataExists);
        return (new GuardedServiceSettingRepository(inner.Object, orders.Object), inner, orders);
    }

    [Fact]
    public async Task MasterDeaktivieren_MitDaten_WirftUndSchreibtNicht()
    {
        var (repo, inner, _) = Build(currentMaster: "true", dataExists: true);
        var act = () => repo.UpsertAsync(HierarchischeStrukturKeys.Master, "false");
        await act.Should().ThrowAsync<HierarchischeStrukturGuardException>();
        inner.Verify(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task MasterAktivieren_OhneDaten_SchreibtDurch()
    {
        var (repo, inner, _) = Build(currentMaster: "false", dataExists: false);
        await repo.UpsertAsync(HierarchischeStrukturKeys.Master, "true", "FA-Hierarchie", "d");
        inner.Verify(r => r.UpsertAsync(HierarchischeStrukturKeys.Master, "true", "FA-Hierarchie", "d"), Times.Once);
    }

    [Fact]
    public async Task UngeschuetzterKey_SchreibtImmerDurch()
    {
        var (repo, inner, _) = Build(currentMaster: "true", dataExists: true);
        await repo.UpsertAsync("Sync:BomCacheEnabled", "true");
        inner.Verify(r => r.UpsertAsync("Sync:BomCacheEnabled", "true", null, null), Times.Once);
    }

    [Fact]
    public async Task MasterLoeschen_MitDaten_Wirft()
    {
        var (repo, _, _) = Build(currentMaster: "true", dataExists: true);
        var act = () => repo.DeleteAsync(HierarchischeStrukturKeys.Master);
        await act.Should().ThrowAsync<HierarchischeStrukturGuardException>();
    }
}
```

- [ ] **Step 2: Test rot verifizieren**

Run: `dotnet test IdealAkeWms.Tests --filter GuardedServiceSettingRepositoryTests`
Expected: FAIL (Klasse fehlt).

- [ ] **Step 3: Decorator implementieren**

```csharp
using IdealAkeWms.Models;
using IdealAkeWms.Services.HierarchischeStruktur;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// Durchsetzungs-Decorator: einziger Choke-Point für Schreibversuche auf die
/// geschützten FA-Hierarchie-Keys (Master + Auto-Erledigt). Alle vier
/// ServiceSettings-Schreibwege (SaveSettings/Create/Edit/Delete) laufen durch
/// <c>UpsertAsync</c>/<c>DeleteAsync</c> — hier greift der Wächter live (ADR 0001,
/// Muster <c>CachedSettingRepository</c>). Reine Lese-Methoden reichen durch.
/// </summary>
public sealed class GuardedServiceSettingRepository : IServiceSettingRepository
{
    private readonly IServiceSettingRepository _inner;
    private readonly IProductionOrderRepository _productionOrders;

    public GuardedServiceSettingRepository(
        IServiceSettingRepository inner,
        IProductionOrderRepository productionOrders)
    {
        _inner = inner;
        _productionOrders = productionOrders;
    }

    public Task<List<ServiceSetting>> GetAllAsync() => _inner.GetAllAsync();
    public Task<List<ServiceSetting>> GetByCategoryAsync(string category) => _inner.GetByCategoryAsync(category);
    public Task<string?> GetValueAsync(string key) => _inner.GetValueAsync(key);

    public async Task UpsertAsync(string key, string value, string? category = null, string? description = null)
    {
        if (HierarchischeStrukturKeys.IsGuarded(key))
            await EnsureAllowedAsync(key, ParseBool(value));
        await _inner.UpsertAsync(key, value, category, description);
    }

    public async Task DeleteAsync(string key)
    {
        // Löschen eines geschützten Keys = Rückfall auf Default (false) = Deaktivieren.
        if (HierarchischeStrukturKeys.IsGuarded(key))
            await EnsureAllowedAsync(key, requestedValue: false);
        await _inner.DeleteAsync(key);
    }

    private async Task EnsureAllowedAsync(string key, bool requestedValue)
    {
        var current = ParseBool(await _inner.GetValueAsync(key));
        var dataExists = await _productionOrders.HierarchicalDataExistsAsync();
        var decision = HierarchischeStrukturGuard.Evaluate(
            HierarchischeStrukturKeys.Map(key), current, requestedValue, dataExists);
        if (!decision.Allowed)
            throw new HierarchischeStrukturGuardException(decision.RejectionReason!);
    }

    private static bool ParseBool(string? v) =>
        string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(v, "on", StringComparison.OrdinalIgnoreCase)
        || v == "1";
}
```

- [ ] **Step 4: Test grün verifizieren**

Run: `dotnet test IdealAkeWms.Tests --filter GuardedServiceSettingRepositoryTests`
Expected: PASS (4 Tests).

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Data/Repositories/GuardedServiceSettingRepository.cs IdealAkeWms.Tests/Data/GuardedServiceSettingRepositoryTests.cs
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe B (4/8) - GuardedServiceSettingRepository (Choke-Point-Decorator)"
```

---

### Task 5: Katalog-Key, SyncLog-Service, DI-Verdrahtung

**Files:**
- Modify: `IdealAkeWms/Models/ServiceSettingDefinitions.cs`
- Modify: `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs`
- Modify: `IdealAkeWms/Program.cs`

- [ ] **Step 1: Master-Key im Katalog (Kategorie „FA-Hierarchie", nach den Info-View-Keys, Zeile ~38)**

```csharp
        new("ProduktionsauftragHierarchisch",        ServiceSettingType.Bool,   "false", "FA-Hierarchie", "EINWEGTOR: schaltet die Materialisierung der Struktur nach ProductionOrders scharf (Sub-FAs werden rückmeldefähig). Nach dem ersten hierarchischen Import nicht mehr über die Anwendung deaktivierbar. Umschalten NUR über die Seite 'FA-Hierarchie-Umstellung' — hier read-only."),
```

- [ ] **Step 2: SyncLog-Service-Konstante**

In `SyncLogServices.cs` bei den übrigen Konstanten:
```csharp
    public const string HierarchieUmstellung = "HierarchieUmstellung"; // Audit: Master-Einwegtor umgelegt (Teil 7)
```
und in das `All`-Array aufnehmen (nach `FaHierarchy`):
```csharp
        SageLagerbuchung, FaHierarchy, HierarchieUmstellung,
```

- [ ] **Step 3: DI — Decorator + Status-Singleton (Program.cs)**

`IServiceSettingRepository`-Registrierung (Zeile 81) ersetzen durch:
```csharp
builder.Services.AddScoped<ServiceSettingRepository>();
builder.Services.AddScoped<IServiceSettingRepository>(sp =>
    new IdealAkeWms.Data.Repositories.GuardedServiceSettingRepository(
        sp.GetRequiredService<ServiceSettingRepository>(),
        sp.GetRequiredService<IProductionOrderRepository>()));
builder.Services.AddSingleton<IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturStatus>();
```
> Prüfen, dass `IProductionOrderRepository` vor dieser Zeile registriert ist (Reihenfolge bei `AddScoped`-Factory egal, da erst bei Auflösung gebraucht).

- [ ] **Step 4: Startup-Refresh des Anzeige-Zustands**

Nach `var app = builder.Build();` (bei den übrigen Startup-Seeds, im selben `using var scope`-Block oder eigenem):
```csharp
// FA-Hierarchie-Anzeigezustand initial befüllen (Freigabe-Antwort 3: Refresh am Start).
await app.Services.GetRequiredService<
    IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturStatus>().RefreshAsync();
```
> Falls der Seed-Block synchron ist: den bestehenden Block-Stil übernehmen (`.GetAwaiter().GetResult()` nur wenn dort schon so genutzt).

- [ ] **Step 5: Build + Drift-Guard-Test**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj && dotnet test IdealAkeWms.Tests --filter ServiceSetting`
Expected: 0 Fehler; Katalog-/Drift-Tests grün (neuer Key ist katalogisiert + wird geseedet).

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Models/ServiceSettingDefinitions.cs IdealAkeWms/Services/SyncLogger/SyncLogServices.cs IdealAkeWms/Program.cs
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe B (5/8) - Master-Key + SyncLog-Service + DI (Decorator/Status)"
```

---

### Task 6: Umschalt-Seite (Controller + ViewModel + View)

**Files:**
- Create: `IdealAkeWms/Models/ViewModels/HierarchieUmstellungViewModel.cs`
- Create: `IdealAkeWms/Controllers/HierarchieUmstellungController.cs`
- Create: `IdealAkeWms/Views/HierarchieUmstellung/Index.cshtml`

- [ ] **Step 1: ViewModel**

```csharp
namespace IdealAkeWms.Models.ViewModels;

public sealed class HierarchieUmstellungViewModel
{
    public bool MasterEnabled { get; init; }
    /// <summary>Gecachter Anzeige-Zustand: hierarchische Daten liegen vor -> Master gesperrt.</summary>
    public bool IsLocked { get; init; }
    public DateTimeOffset? StatusRefreshedUtc { get; init; }
}
```

- [ ] **Step 2: Controller**

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.HierarchischeStruktur;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// Teil 7 — dedizierte Umschalt-Seite für das FA-Hierarchie-Einwegtor
/// (<see cref="HierarchischeStrukturKeys.Master"/>). Einziger sanktionierter UI-Schreibweg;
/// die generische ServiceSettings-Maske zeigt den Master nur read-only. Die eigentliche
/// Sicherung ist der <see cref="GuardedServiceSettingRepository"/>-Choke-Point — dieser
/// Controller validiert zusätzlich vorab für eine saubere Meldung und protokolliert.
/// </summary>
[RequireAdminAccess]
public class HierarchieUmstellungController : Controller
{
    private readonly IServiceSettingRepository _settings;
    private readonly IProductionOrderRepository _productionOrders;
    private readonly HierarchischeStrukturStatus _status;
    private readonly ISyncLogger _syncLogger;
    private readonly ICurrentUserService _currentUser;

    public HierarchieUmstellungController(
        IServiceSettingRepository settings,
        IProductionOrderRepository productionOrders,
        HierarchischeStrukturStatus status,
        ISyncLogger syncLogger,
        ICurrentUserService currentUser)
    {
        _settings = settings;
        _productionOrders = productionOrders;
        _status = status;
        _syncLogger = syncLogger;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index()
    {
        var value = await _settings.GetValueAsync(HierarchischeStrukturKeys.Master);
        return View(new HierarchieUmstellungViewModel
        {
            MasterEnabled = ParseBool(value),
            IsLocked = _status.HierarchicalDataExists,
            StatusRefreshedUtc = _status.LastRefreshedUtc,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Enable() => SetMasterAsync(true);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Disable() => SetMasterAsync(false);

    private async Task<IActionResult> SetMasterAsync(bool target)
    {
        var current = ParseBool(await _settings.GetValueAsync(HierarchischeStrukturKeys.Master));
        if (current == target)
        {
            TempData["WarningMessage"] = "Keine Änderung: der Schalter stand bereits so.";
            return RedirectToAction(nameof(Index));
        }

        // Live-Sperrprüfung für eine saubere Meldung (Guard-Decorator sichert zusätzlich hart ab).
        var dataExists = await _productionOrders.HierarchicalDataExistsAsync();
        var decision = HierarchischeStrukturGuard.Evaluate(
            GuardedSetting.Master, current, target, dataExists);
        if (!decision.Allowed)
        {
            await using var rejectRun = await _syncLogger.BeginRunAsync(SyncLogServices.HierarchieUmstellung);
            await rejectRun.LogWarningAsync(
                $"Master-Umschaltung abgelehnt ({(target ? "aktivieren" : "deaktivieren")}) durch {_currentUser.WindowsUsername}: {decision.RejectionReason}");
            await rejectRun.FinishSuccessAsync();
            TempData["WarningMessage"] = decision.RejectionReason;
            return RedirectToAction(nameof(Index));
        }

        await _settings.UpsertAsync(HierarchischeStrukturKeys.Master, target ? "true" : "false",
            "FA-Hierarchie", "EINWEGTOR (siehe Umschalt-Seite)");

        // Beim Einschalten einen ggf. aktiven Auto-Erledigt-Schalter abschalten (defensiv;
        // No-op solange der Key (Etappe D) nicht existiert) + protokollieren.
        var autoOff = false;
        if (target && ParseBool(await _settings.GetValueAsync(HierarchischeStrukturKeys.AutoErledigt)))
        {
            await _settings.UpsertAsync(HierarchischeStrukturKeys.AutoErledigt, "false", "Sync",
                "Automatisch abgeschaltet beim Umlegen des FA-Hierarchie-Masters.");
            autoOff = true;
        }

        await using (var run = await _syncLogger.BeginRunAsync(SyncLogServices.HierarchieUmstellung))
        {
            await run.LogInfoAsync(
                $"FA-Hierarchie-Master auf '{(target ? "aktiviert" : "deaktiviert")}' gesetzt durch {_currentUser.WindowsUsername}."
                + (autoOff ? " Auto-Erledigt automatisch abgeschaltet." : string.Empty));
            await run.FinishSuccessAsync();
        }

        await _status.RefreshAsync();
        TempData["SuccessMessage"] = target
            ? "Umstellung auf hierarchische Produktionsaufträge aktiviert."
            : "Umstellung deaktiviert (es lagen noch keine hierarchischen Daten vor).";
        return RedirectToAction(nameof(Index));
    }

    private static bool ParseBool(string? v) =>
        string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
}
```
> `ICurrentUserService`-Property für den Windows-Login prüfen (`WindowsUsername` o.ä.) und den realen Namen einsetzen.

- [ ] **Step 3: View — konsistent mit Bootstrap-Hausstil, Gefahren-/Bestätigungs-Signatur**

`Views/HierarchieUmstellung/Index.cshtml`:
```cshtml
@model IdealAkeWms.Models.ViewModels.HierarchieUmstellungViewModel
@{
    ViewData["Title"] = "FA-Hierarchie-Umstellung";
}

<div class="d-flex justify-content-between align-items-center flex-wrap gap-2 mb-3">
    <h2 class="page-header mb-0">Umstellung auf hierarchische Produktionsaufträge</h2>
    <a asp-controller="ServiceSettings" asp-action="Index" class="btn btn-outline-secondary btn-sm">Zu den Service-Einstellungen</a>
</div>

@if (TempData["SuccessMessage"] != null)
{
    <div class="alert alert-success">@TempData["SuccessMessage"]</div>
}
@if (TempData["WarningMessage"] != null)
{
    <div class="alert alert-warning">@TempData["WarningMessage"]</div>
}

<div class="card mb-3">
    <div class="card-body">
        <p class="mb-2">
            Dieser Schalter schaltet die <strong>Materialisierung der FA-Struktur nach
            Produktionsaufträgen</strong> scharf: Sub-FAs werden zu echten Produktionsaufträgen
            und damit rückmeldefähig (Arbeitsgänge, Teileverfolgung, BDE).
        </p>
        <p class="mb-0 text-body-secondary">
            Solange noch keine hierarchischen Daten vorliegen, lässt sich der Schalter in beide
            Richtungen ändern. Mit dem ersten hierarchischen Import greift die Sperre.
        </p>
    </div>
</div>

<div class="card mb-3">
    <div class="card-header fw-semibold">Aktueller Zustand</div>
    <div class="card-body">
        <p class="mb-3">
            Status:
            @if (Model.MasterEnabled)
            {
                <span class="badge text-bg-success">Hierarchisch aktiviert</span>
            }
            else
            {
                <span class="badge text-bg-secondary">Flach (AKE-Standard)</span>
            }
            @if (Model.IsLocked)
            {
                <span class="badge text-bg-dark ms-1">gesperrt — hierarchische Daten vorhanden</span>
            }
        </p>

        @if (Model.IsLocked)
        {
            <div class="alert alert-secondary mb-0">
                <strong>Nicht mehr über die Anwendung umkehrbar.</strong>
                Es liegen Aufträge mit Sub-FA-Struktur vor. Ein Rückbau zur flachen Struktur ist
                nur außerhalb der Anwendung möglich — siehe
                <a href="https://github.com" onclick="return false;" class="alert-link">Runbook
                <code>docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md</code></a> (Datenbereinigung, Backup zwingend).
            </div>
        }
        else if (Model.MasterEnabled)
        {
            <form asp-action="Disable" method="post" class="d-inline">
                @Html.AntiForgeryToken()
                <button type="submit" class="btn btn-outline-secondary">Wieder deaktivieren</button>
            </form>
            <span class="form-text ms-2">Möglich, solange keine hierarchischen Daten vorliegen.</span>
        }
        else
        {
            <button type="button" class="btn btn-danger" data-bs-toggle="modal" data-bs-target="#confirmEnableModal">
                Auf hierarchische Produktionsaufträge umstellen
            </button>
        }
    </div>
</div>

@if (!Model.IsLocked && !Model.MasterEnabled)
{
    <div class="modal fade" id="confirmEnableModal" tabindex="-1" aria-labelledby="confirmEnableLabel" aria-hidden="true">
        <div class="modal-dialog">
            <div class="modal-content border-danger">
                <div class="modal-header bg-danger text-white">
                    <h5 class="modal-title" id="confirmEnableLabel">Umstellung bestätigen</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Schließen"></button>
                </div>
                <div class="modal-body">
                    <p class="mb-0">
                        Umstellung auf hierarchische Produktionsaufträge. <strong>Nach dem ersten
                        hierarchischen Import ist eine Rückkehr zur flachen Struktur nicht mehr möglich.</strong>
                        Vorher auf einer Datenbank-Kopie testen. Wirklich umstellen?
                    </p>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Abbrechen</button>
                    <form asp-action="Enable" method="post" class="d-inline">
                        @Html.AntiForgeryToken()
                        <button type="submit" class="btn btn-danger">Jetzt umstellen</button>
                    </form>
                </div>
            </div>
        </div>
    </div>
}
```
> Der Runbook-Link zeigt in der App auf keine gehostete URL — der Pfad wird als `code` genannt (Runbook liegt im Repo/Deploy-`docs/`). Kontrast: `text-bg-*`-Badges und `btn-danger`/`bg-danger`-Weiß erfüllen WCAG AA.

- [ ] **Step 4: Build**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: 0 Fehler.

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Controllers/HierarchieUmstellungController.cs IdealAkeWms/Models/ViewModels/HierarchieUmstellungViewModel.cs IdealAkeWms/Views/HierarchieUmstellung/
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe B (6/8) - Umschalt-Seite (Controller/VM/View) + Audit-SyncLog"
```

---

### Task 7: Generische Maske read-only + Guard-Ablehnung abfangen

**Files:**
- Modify: `IdealAkeWms/Views/ServiceSettings/Index.cshtml`
- Modify: `IdealAkeWms/Controllers/ServiceSettingsController.cs`

- [ ] **Step 1: Master read-only in der generischen Maske**

In `Views/ServiceSettings/Index.cshtml` den Bool-Zweig (Zeile ~68) um einen Sonderfall **vor** dem `if (item.Type == Bool)` erweitern:
```cshtml
                        @if (item.Key == IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master)
                        {
                            var masterOn = string.Equals(item.Value, "true", StringComparison.OrdinalIgnoreCase);
                            <div class="mt-1">
                                <span class="badge @(masterOn ? "text-bg-success" : "text-bg-secondary")">
                                    @(masterOn ? "Hierarchisch aktiviert" : "Flach (AKE-Standard)")
                                </span>
                                <a asp-controller="HierarchieUmstellung" asp-action="Index" class="ms-2">Umschalten (eigene Seite)</a>
                            </div>
                            <small class="text-muted d-block mt-1">Einwegtor — hier nur Anzeige. Änderung nur über die Umstellungs-Seite.</small>
                        }
                        else if (item.Type == ServiceSettingType.Bool)
```
> Kein `<input name="settings[...]">` für den Master → er wird nie mitgepostet. Der `@using`-Alias oben in der Datei ist optional; hier voll qualifiziert.

- [ ] **Step 2: Guard-Ablehnung in SaveSettings abfangen + protokollieren (AK 2)**

`ServiceSettingsController` bekommt `ISyncLogger`+`ICurrentUserService` per Ctor. Den `UpsertAsync`-Aufruf-Block in `SaveSettings` in try/catch hüllen:
```csharp
            try
            {
                switch (def.Type)
                {
                    // ... bestehender switch unverändert (Upsert-Aufrufe drin) ...
                }
            }
            catch (IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturGuardException ex)
            {
                ModelState.AddModelError(key, ex.Message);
                await using var run = await _syncLogger.BeginRunAsync(
                    IdealAkeWms.Services.SyncLogger.SyncLogServices.HierarchieUmstellung);
                await run.LogWarningAsync(
                    $"Technischer Schreibversuch auf geschützten Key '{key}' über die generische Maske abgelehnt (durch {_currentUser.WindowsUsername}): {ex.Message}");
                await run.FinishSuccessAsync();
            }
```
> Ctor + Felder `_syncLogger`/`_currentUser` ergänzen (siehe `HierarchieUmstellungController` für die Typen). `SaveSettings` bereits `async` — kompatibel.

- [ ] **Step 3: Build**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: 0 Fehler.

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Views/ServiceSettings/Index.cshtml IdealAkeWms/Controllers/ServiceSettingsController.cs
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe B (7/8) - Master read-only in generischer Maske + Guard-Ablehnung protokolliert"
```

---

### Task 8: Runbook + README-Verlinkung

**Files:**
- Create: `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md`
- Modify: `README.md`

- [ ] **Step 1: Runbook schreiben** (Vorbedingungen inkl. Backup, betroffene Tabellen, Bereinigungsschritt, ehrliche Datenverlust-Warnung; Formulierung „nicht über die Anwendung umkehrbar")

`docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md` mit Abschnitten:
- **Wann dieses Runbook gilt** — Master `ProduktionsauftragHierarchisch` wurde aktiviert und es liegen bereits Aufträge mit `OrderNumber <> SubOrderNumber` vor; ein Rückbau zur flachen Struktur ist gewünscht.
- **Warnung** — der Rückbau ist **nicht über die Anwendung umkehrbar**; er ist eine bewusste Datenbereinigung außerhalb der App und **verwirft die Sub-FA-Materialisierung inkl. daran hängender Rückmeldungen** (Arbeitsgänge/BDE auf Sub-FA-Ebene).
- **Vorbedingungen** — DB-Backup zwingend; Windows-Service gestoppt; niemand arbeitet auf den betroffenen FAs.
- **Betroffene Tabellen** — `ProductionOrders` (Zeilen mit `OrderNumber <> SubOrderNumber` sowie die Sub-Ebenen), abhängige Rückmelde-/Arbeitsgang-Tabellen (Teil 8).
- **Bereinigungsschritt** — skizziertes SQL (in eigenem Wartungsfenster, nach Backup): die materialisierten Sub-Zeilen entfernen bzw. auf den flachen Zustand zurückführen, danach Master auf `false` setzen; Reihenfolge + Guard-Umgehung (direkt in DB) benannt.
- **Danach** — Service-Neustart, Anzeige-Zustand refresht sich beim Start.

- [ ] **Step 2: README-Verlinkung**

In `README.md` (Betriebs-/Runbook-Abschnitt) eine Zeile ergänzen:
```markdown
- **FA-Hierarchie-Rückbau (Einwegtor):** [docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md](docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md) — Rückweg aus dem hierarchischen Modus (nicht über die Anwendung umkehrbar).
```

- [ ] **Step 3: Commit**

```bash
git add docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md README.md
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe B (8/8) - Rückbau-Runbook + README-Verlinkung"
```

---

### Task 9: Gesamt-Build + Test + Testszenarien (docs)

**Files:**
- Modify: `docs/TESTSZENARIEN.md` (neues/erweitertes Kapitel Teil 7 Einwegtor)

- [ ] **Step 1: Volltest**

Run: `dotnet build IdealAkeWms.slnx && dotnet test`
Expected: Web + Service grün, 0 Fehler.

- [ ] **Step 2: Testszenarien ergänzen** — manuelle Szenarien: Master einschalten ohne Daten (Modal + Erfolg + noch rückgängig); Master-Anzeige read-only in generischer Maske (kein Bedienelement); technischer Schreibversuch auf Master über generische Maske → abgelehnt + SyncLog-Eintrag; Sperr-Anzeige nach (simulierten) hierarchischen Daten; Audit-Eintrag „HierarchieUmstellung" prüfbar; Runbook erreichbar/verlinkt.

- [ ] **Step 3: Commit**

```bash
git add docs/TESTSZENARIEN.md
git commit -m "epic 2026-08-07-ideal-teile-1-5: Teil 7 Etappe B - Testszenarien Einwegtor (docs)"
```

---

## Nach dem Code (Hauptcheckout — NICHT im Worktree)

- Etappen-Tabelle Teil 7 (Spec + `secondbrain/aufgaben/2026-07-29-standort-ideal-teil-7.md`): Etappe B → erledigt + Commit-Hash; Umsetzungsnotiz + Schalter-Inventur-Entscheidung.
- `secondbrain/tests/testszenarien-index.md` nachziehen.
- Etappen C–E bleiben offen → Status Teil 7 bleibt `InUmsetzung`. Kein qa-agent (nur nach ALLEN Etappen), kein Merge, kein Push.

## Nicht in Etappe B (bewusst)

- Auto-Erledigt-Katalog-Key + Fold-2-Verdrahtung + datengetriebener Skip + hierarchische Tests → **Etappe D**.
- Materialisierungs-Sync + `HierarchischeStrukturStatus.RefreshAsync()`-Aufruf am Sync-Ende → **Etappe C**.
- Version-Bump/Changelog/Brain-Dauerwissen → **Etappe E** (ein Bump fürs Bündel).
