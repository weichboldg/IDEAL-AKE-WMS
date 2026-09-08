# BOM-Bridge hierarchisch — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Im hierarchischen Modus (IDEAL, Master `ProduktionsauftragHierarchisch=true`) liefert eine zweite Implementierung der bestehenden `IBomRepository`/`IBomCacheRepository`-Schnittstelle die Stueckliste direkt aus `FaHierarchyNode` — Picking-Workflow, Vorbau-Stueckliste, Artikelinfo laufen damit unveraendert; die AKE-Heuristiken (Coating/WorkStep/BomCache-Sync) werden hart abgeschaltet; AKE (Master false) bleibt bit-identisch.

**Architecture:** Ein per-Aufruf entscheidender `BomRepositoryMasterSwitch` (beide Interfaces) waehlt zwischen dem unveraenderten AKE-Pfad (`CachedBomRepository`→`BomRepository`→`BomCacheRepository`) und dem neuen `FaHierarchyBomRepository`. Der Schluessel wird `BomKey(ArticleNumber, SubOrderNumber, OrderNumber)`, der Scope `BomScope` (DirectChildren/FullStructure); das Ergebnis traegt sein Mengen-Verhalten selbst (`MengeIstAuftragsmenge`). `FullStructure` nutzt den bestehenden `FaHierarchyTreeBuilder` (kein zweiter Baum-Walker) und baut `Position` als rekursiven Pfad (`3.7.2`), sodass die vorhandene Baum-Logik in `Bom.cshtml` unveraendert bleibt. Service-seitig kapselt `IHierarchicalModeReader` den DB-first-Master-Read, damit die Klasse-D-Gates ohne SQL Server testbar sind.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10 (InMemory in Tests), xUnit + FluentAssertions + Moq, Bootstrap 5, vorhandene `column-preferences.js`/`table-filter.js`.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec.md` (Hauptcheckout; Frontmatter jetzt `status: InUmsetzung`). Freigabe-Antwort 1 ist verbindlich: rekursiver Pfad + Bedingung (a) Sage-Position sichtbar + (b) Kollisionen erkennen.

## Global Constraints

- **Worktree:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Ausgangs-HEAD `a8d75de`. Kein neuer Worktree, kein Merge, kein Push. Alle `secondbrain/`-Aenderungen NUR im Hauptcheckout `C:\git\IDEAL-AKE-WMS\secondbrain\` (Task 6).
- **AKE bit-identisch (AK 1):** Bei Master `false` keine Verhaltensaenderung. Die flache Implementierung liest aus `BomKey` nur `ArticleNumber` und ignoriert `SubOrderNumber`/`OrderNumber`/`BomScope`. Keine neue Spalte im Flachmodus (nur bei `Model.Hierarchical` rendern).
- **`BomItem` bleibt im Feldbestand unveraendert.** Grund: `BomRepository` mappt die AKE-View per `SqlQueryRaw<BomItem>` — jede neue Property ohne Spalte wuerde dort mit „required column not present" brechen. Hierarchische Zusatzfelder leben in der Ableitung `FaHierarchyBomItem : BomItem`.
- **Keine Migration, keine neue Rolle, kein neuer Settings-Key.** Master-Read ueber `HierarchischeStrukturKeys.Master` (`"ProduktionsauftragHierarchisch"`).
- **`ISyncLogger` bleibt LETZTER Konstruktor-Parameter** der Service-Klassen (Fallstrick). Neue Parameter werden davor eingefuegt.
- **Sprachregel:** Code Englisch, UI-Texte Deutsch. Umlaute in Views sind erlaubt (bestehende Views nutzen sie), in Code-Kommentaren `ue/ae/oe`.
- **Version:** beide `AppVersion.cs` auf `1.36.0`, Date `2026-09-08` (Task 6).
- **Commits:** nach jedem Task `git add <dateien> && git commit -m "wip: bom-bridge …"`; Trailer:
  `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>` und
  `Claude-Session: https://claude.ai/code/session_013T5EmMH4SRpU2z5rwBGTaU`.
  `publish.zip` (unstaged ` D`) NICHT stagen.
- **Tests gruen:** `dotnet build IdealAkeWms.slnx` 0 Fehler; `dotnet test IdealAkeWms.Tests` und `dotnet test IDEALAKEWMSService.Tests` gruen nach jedem Task (Stand vor Plan: Web 1245 +1 skip, Service 232).

**Reihenfolge / Parallelitaet:** Task 1 zuerst (Typen + Signatur). Danach **Task 2 und Task 4 parallel** (unabhaengig). Dann Task 3 (braucht 2), dann Task 5 (braucht 3), dann Task 6.

---

### Task 1: Typen, Mengen-Flag, Signaturwechsel (AKE bit-identisch)

**Files:**
- Create: `IdealAkeWms/Models/BomKey.cs`
- Create: `IdealAkeWms/Models/BomScope.cs`
- Create: `IdealAkeWms/Services/BomQuantityResolver.cs`
- Modify: `IdealAkeWms/Models/ViewModels/BomViewModels.cs` (Zeile 3 `BomQueryResult`, Zeile 6-19 `BomDataSources`)
- Modify: `IdealAkeWms/Data/Repositories/IBomRepository.cs`, `IBomCacheRepository.cs` (Zeile 13), `BomRepository.cs` (Zeile 22-25), `CachedBomRepository.cs` (Zeile 18-25), `BomCacheRepository.cs` (`GetByArticleNumberAsync`), `HierarchicalBomGuardRepository.cs` (Zeile 28-37, nur Signatur — wird in Task 3 geloescht)
- Modify: `IdealAkeWms/Controllers/PickingController.cs` (Zeile 300, 396, 549, 582, 645), `IdealAkeWms/Services/ReadOnlyBomBuilder.cs` (Zeile 64, 98)
- Test (anpassen): `IdealAkeWms.Tests/Repositories/BomRepositoryCacheFirstTests.cs`, `BomCacheRepositoryTests.cs`, `HierarchicalBomGuardRepositoryTests.cs`, `IdealAkeWms.Tests/Services/ReadOnlyBomBuilderTests.cs`, `IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs`, `FaCompletionControllerTests.cs`, `PickingControllerTests.cs`
- Test (neu): `IdealAkeWms.Tests/Services/BomQuantityResolverTests.cs`

**Interfaces:**
- Produces: `BomKey(string ArticleNumber, string? SubOrderNumber = null, string? OrderNumber = null)` + `BomKey.ForOrder(ProductionOrder)`; `enum BomScope { DirectChildren, FullStructure }` + `BomScopes.ForOrder(ProductionOrder)`; `record BomQueryResult(List<BomItem> Items, string DataSource, bool MengeIstAuftragsmenge = false)`; `BomDataSources.FaHierarchie = "FA-HIERARCHIE"`; `IBomRepository.GetBomItemsAsync(BomKey key, BomScope scope = BomScope.DirectChildren)`; `IBomCacheRepository.GetByArticleNumberAsync(BomKey key)`; `BomQuantityResolver.Resolve(BomQueryResult, BomItem, ProductionOrder) : decimal`.

- [ ] **Step 1: Neue Typen anlegen**

`IdealAkeWms/Models/BomKey.cs`:
```csharp
namespace IdealAkeWms.Models;

/// <summary>
/// Schluessel einer Stuecklisten-Abfrage (BOM-Bridge, Spec 2026-09-08).
/// AKE (flach) liest ausschliesslich <see cref="ArticleNumber"/>; die FA-Hierarchie (IDEAL) braucht
/// <see cref="SubOrderNumber"/> (WELCHER Fertigungsauftrag) und <see cref="OrderNumber"/> (HauptFA =
/// Struktur-Gruppe fuer den gecachten Lookup <c>GetByHauptFaAsync</c>). Die flache Implementierung
/// ignoriert die beiden Zusatzfelder vollstaendig — bit-identisches AKE-Verhalten.
/// </summary>
public sealed record BomKey(string ArticleNumber, string? SubOrderNumber = null, string? OrderNumber = null)
{
    public static BomKey ForOrder(ProductionOrder order) =>
        new(order.ArticleNumber ?? string.Empty, order.SubOrderNumber, order.OrderNumber);
}
```

`IdealAkeWms/Models/BomScope.cs`:
```csharp
namespace IdealAkeWms.Models;

/// <summary>Umfang einer hierarchischen Stuecklisten-Abfrage (BOM-Bridge, Spec Design D).</summary>
public enum BomScope
{
    /// <summary>Direkte Kinder des Sub-FA (Standard, AKE-Aequivalent; Partition fuer Aggregate).</summary>
    DirectChildren,
    /// <summary>Alle Nachfahren ueber alle Ebenen, jeder Knoten einmal, flach mit rekursivem
    /// Positions-Pfad. NUR Anzeige-Scope des Picking-Workflows, nie fuer Aggregate.</summary>
    FullStructure
}

public static class BomScopes
{
    /// <summary>
    /// Fachliche Anforderung 6: Auftrag ist HauptFA (<c>OrderNumber == SubOrderNumber</c>) → komplette
    /// Struktur; Sub-FA → nur eigene Stueckliste. Im Flachmodus gilt die Backfill-Invariante
    /// <c>SubOrderNumber == OrderNumber</c>; die flache Implementierung ignoriert den Scope ohnehin.
    /// </summary>
    public static BomScope ForOrder(ProductionOrder order) =>
        string.Equals(order.SubOrderNumber, order.OrderNumber, StringComparison.Ordinal)
            ? BomScope.FullStructure
            : BomScope.DirectChildren;
}
```

`IdealAkeWms/Services/BomQuantityResolver.cs`:
```csharp
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>
/// Einzige Stelle, die die BOM-Menge auf die Auftragsmenge hochrechnet (Spec Design C).
/// AKE-Cache/-View fuehren <c>Menge</c> je Stueck → × <c>order.Quantity</c>;
/// die FA-Hierarchie liefert <c>Sollmenge</c> bereits als Auftragsgesamtmenge → unveraendert.
/// Das Ergebnis beschreibt sich selbst (<see cref="BomQueryResult.MengeIstAuftragsmenge"/>),
/// keine Master-Abfrage in Verbrauchern.
/// </summary>
public static class BomQuantityResolver
{
    public static decimal Resolve(BomQueryResult result, BomItem item, ProductionOrder order) =>
        result.MengeIstAuftragsmenge ? item.Menge : item.Menge * order.Quantity;
}
```

- [ ] **Step 2: `BomQueryResult` + `BomDataSources` erweitern** (`BomViewModels.cs`)

Zeile 3 ersetzen:
```csharp
public record BomQueryResult(List<BomItem> Items, string DataSource, bool MengeIstAuftragsmenge = false);
```
In `BomDataSources` (nach `HierarchicalUnavailable`, Zeile 18) ergaenzen:
```csharp
    /// <summary>BOM-Bridge (Spec 2026-09-08): Quelle ist die lokale FA-Struktur (FaHierarchyNode).</summary>
    public const string FaHierarchie = "FA-HIERARCHIE";
```

- [ ] **Step 3: Failing test fuer den Resolver schreiben**

`IdealAkeWms.Tests/Services/BomQuantityResolverTests.cs`:
```csharp
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class BomQuantityResolverTests
{
    private static ProductionOrder Order(decimal quantity) => new()
    {
        OrderNumber = "1", SubOrderNumber = "1", Quantity = quantity,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
    };

    [Fact]
    public void Flat_MultipliesByOrderQuantity()
    {
        var result = new BomQueryResult(new List<BomItem>(), BomDataSources.Sage);
        var item = new BomItem { Menge = 2.5m };

        BomQuantityResolver.Resolve(result, item, Order(4m)).Should().Be(10m);
    }

    [Fact]
    public void Hierarchical_KeepsSollmengeUnchanged()
    {
        var result = new BomQueryResult(new List<BomItem>(), BomDataSources.FaHierarchie, MengeIstAuftragsmenge: true);
        var item = new BomItem { Menge = 2.5m };

        BomQuantityResolver.Resolve(result, item, Order(4m)).Should().Be(2.5m);
    }
}
```

- [ ] **Step 4: Test laufen lassen → FAIL (Typ fehlt noch nicht kompiliert = erst Step 1/2 fertig, dann gruen)**

Run: `dotnet test IdealAkeWms.Tests --filter "FullyQualifiedName~BomQuantityResolverTests"`
Expected nach Step 1-2: PASS (2 Tests).

- [ ] **Step 5: Signaturen umstellen**

`IBomRepository.cs`:
```csharp
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Data.Repositories;

public interface IBomRepository
{
    /// <summary>Stueckliste des Auftrags. Flach (AKE): nur <see cref="BomKey.ArticleNumber"/> zaehlt,
    /// <paramref name="scope"/> wird ignoriert. Hierarchisch (IDEAL): Sub-FA + Scope (Spec Design A/D).</summary>
    Task<BomQueryResult> GetBomItemsAsync(BomKey key, BomScope scope = BomScope.DirectChildren);
}
```
`IBomCacheRepository.cs` Zeile 13: `Task<BomQueryResult?> GetByArticleNumberAsync(BomKey key);` (+ `using IdealAkeWms.Models;`). Alle anderen sechs Methoden unveraendert.

`BomRepository.cs` Zeile 22-25:
```csharp
    public async Task<BomQueryResult> GetBomItemsAsync(BomKey key, BomScope scope = BomScope.DirectChildren)
    {
        // Flach (AKE): nur die Artikelnummer zaehlt, Scope/SubOrderNumber werden ignoriert (bit-identisch).
        var productionOrderArticleNumber = key.ArticleNumber;
        // 1. Cache-First
        var cached = await _bomCacheRepository.GetByArticleNumberAsync(key);
```
(Rest der Methode unveraendert, `productionOrderArticleNumber` wird weiter verwendet.)

`CachedBomRepository.cs` Zeile 18-25:
```csharp
    public async Task<BomQueryResult> GetBomItemsAsync(BomKey key, BomScope scope = BomScope.DirectChildren)
    {
        var cacheKey = $"bom:{key.ArticleNumber}";

        if (_cache.TryGetValue(cacheKey, out BomQueryResult? cached) && cached != null)
            return cached;

        var result = await _inner.GetBomItemsAsync(key, scope);
```
`BomCacheRepository.GetByArticleNumberAsync(BomKey key)`: Rumpf unveraendert, `articleNumber` → `key.ArticleNumber` (erste Zeile: `var articleNumber = key.ArticleNumber;`).
`HierarchicalBomGuardRepository.cs`: Signatur auf `(BomKey key, BomScope scope = BomScope.DirectChildren)`, Delegation `_inner.GetBomItemsAsync(key, scope)` (Datei wird in Task 3 entfernt).

- [ ] **Step 6: Aufrufer mechanisch umstellen (Scope-Regel + Resolver schon hier, Verhalten flach identisch)**

`PickingController.cs`:
- Zeile 300: `var bomResult = await _bomRepository.GetBomItemsAsync(BomKey.ForOrder(order), BomScopes.ForOrder(order));`
- Zeile 396: `Menge = BomQuantityResolver.Resolve(bomResult, bom, order),`
- Zeile 549: `var bomResult = await _bomRepository.GetBomItemsAsync(BomKey.ForOrder(order), BomScopes.ForOrder(order));` und direkt danach `var bomItems = bomResult.Items;`
- Zeile 582: `Menge = BomQuantityResolver.Resolve(bomResult, bom, order),`
- Zeile 645: `var bomItems = (await _bomRepository.GetBomItemsAsync(BomKey.ForOrder(order))).Items;`
`ReadOnlyBomBuilder.cs`:
- Zeile 64: `var bomResult = await _bomRepository.GetBomItemsAsync(BomKey.ForOrder(order), BomScopes.ForOrder(order));`
- Zeile 98: `Menge = BomQuantityResolver.Resolve(bomResult, bom, order),`
(`using IdealAkeWms.Models;` bzw. `using IdealAkeWms.Services;` ergaenzen, wo noetig.)

- [ ] **Step 7: Bestehende Tests mechanisch anpassen**

- `BomRepositoryCacheFirstTests.cs`: `cacheMock.Setup(c => c.GetByArticleNumberAsync(It.Is<BomKey>(k => k.ArticleNumber == "ART1")))`, Aufruf `repo.GetBomItemsAsync(new BomKey("ART1"))`, `Verify(... It.Is<BomKey>(k => k.ArticleNumber == "ART1") ..., Times.Once)`.
- `BomCacheRepositoryTests.cs` Zeile 35/52: `repo.GetByArticleNumberAsync(new BomKey("DOES_NOT_EXIST"))` bzw. `new BomKey("ART1")`.
- `HierarchicalBomGuardRepositoryTests.cs`: `GetBomItemsAsync("ART-1")` → `GetBomItemsAsync(new BomKey("ART-1"))`; `_inner.Verify(r => r.GetBomItemsAsync(It.IsAny<BomKey>(), It.IsAny<BomScope>()), …)`; Setups analog.
- `ReadOnlyBomBuilderTests.cs:28`, `FaWorklistControllerTests.cs:53`, `FaCompletionControllerTests.cs:41`, `PickingControllerTests.cs:114`: `Setup(b => b.GetBomItemsAsync(It.IsAny<BomKey>(), It.IsAny<BomScope>()))`.

- [ ] **Step 8: Build + alle Web-Tests**

Run: `dotnet build IdealAkeWms.slnx` → 0 Fehler. `dotnet test IdealAkeWms.Tests` → alle gruen (1245 + 2 neue). `dotnet test IDEALAKEWMSService.Tests` → 232 gruen.

- [ ] **Step 9: Commit**

```bash
git add IdealAkeWms/Models/BomKey.cs IdealAkeWms/Models/BomScope.cs IdealAkeWms/Services/BomQuantityResolver.cs IdealAkeWms/Models/ViewModels/BomViewModels.cs IdealAkeWms/Data/Repositories/IBomRepository.cs IdealAkeWms/Data/Repositories/IBomCacheRepository.cs IdealAkeWms/Data/Repositories/BomRepository.cs IdealAkeWms/Data/Repositories/CachedBomRepository.cs IdealAkeWms/Data/Repositories/BomCacheRepository.cs IdealAkeWms/Data/Repositories/HierarchicalBomGuardRepository.cs IdealAkeWms/Controllers/PickingController.cs IdealAkeWms/Services/ReadOnlyBomBuilder.cs IdealAkeWms.Tests
git commit -m "wip: bom-bridge Task 1 — BomKey/BomScope/MengeIstAuftragsmenge + Signaturwechsel (AKE bit-identisch)"
```

---

### Task 2: `FaHierarchyBomRepository` (beide Interfaces) + Tests

**Files:**
- Create: `IdealAkeWms/Models/FaHierarchyBomItem.cs`
- Create: `IdealAkeWms/Data/Repositories/FaHierarchyBomRepository.cs`
- Test: `IdealAkeWms.Tests/Repositories/FaHierarchyBomRepositoryTests.cs`

**Interfaces:**
- Consumes: Task 1 (`BomKey`, `BomScope`, `BomQueryResult(…, MengeIstAuftragsmenge)`, `BomDataSources.FaHierarchie`); vorhanden: `IFaHierarchyNodeRepository.GetByHauptFaAsync(int)`/`GetAllAsync()`, `FaHierarchyTreeBuilder.Build(nodes, orderInfos, depthCap)` → `List<FaHierarchyStructure>` mit `Roots[0]`, `Children`, `Level`, `IsOrphan`, `HasError`, `ErrorMessage`; `FaHierarchyNode` (`HauptFA:int`, `VaterFA:int?`, `SubFA:int`, `Position:int?`, `HauptArtnr`, `Artnr`, `Bezeichnung1/2`, `Artikelgruppe`, `Hauptlagerplatz`, `Kommissionieren`, `Sollmenge:decimal`, `Beschaffungsartikel:bool`).
- Produces: `FaHierarchyBomItem : BomItem { bool IsBaugruppe; string? Kommissionieren; string? Hauptlagerplatz; int Ebene; string? VaterSubFa; string? SagePosition; bool IsWaise; bool PositionsKollision; }`; `FaHierarchyBomRepository : IBomRepository, IBomCacheRepository` mit Ctor `(IFaHierarchyNodeRepository nodes, FaHierarchyTreeBuilder treeBuilder, ILogger<FaHierarchyBomRepository> logger)`. Semantik der Artikelinfo-Methoden: `GetDeviceArticleNumbersByComponentAsync` liefert die **SubOrderNumbers der Eltern-Sub-FAs** (Strings), `GetComponentMengePerDeviceAsync` `Dictionary<ElternSubFa, Σ Sollmenge>`.

- [ ] **Step 1: `FaHierarchyBomItem` anlegen**

`IdealAkeWms/Models/FaHierarchyBomItem.cs`:
```csharp
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Models;

/// <summary>
/// Stuecklisten-Zeile aus der FA-Hierarchie (BOM-Bridge). Erbt die AKE-Felder von <see cref="BomItem"/>
/// (das selbst unveraendert bleibt — es wird per <c>SqlQueryRaw</c> auf die AKE-View gemappt, jede neue
/// Property dort braeche das Mapping) und traegt die hierarchischen Zusatzfelder.
/// </summary>
public sealed class FaHierarchyBomItem : BomItem
{
    /// <summary>Zeile ist eine Baugruppe mit eigenem Sub-FA (<c>FaHierarchyNode.SubFA != 0</c>).</summary>
    public bool IsBaugruppe { get; set; }
    public string? Kommissionieren { get; set; }
    public string? Hauptlagerplatz { get; set; }
    /// <summary>1 = direktes Kind des abgefragten FA; bei FullStructure die Tiefe ab der Wurzel.</summary>
    public int Ebene { get; set; }
    /// <summary>SubFA des unmittelbaren Elternknotens (bei DirectChildren: der abgefragte FA).</summary>
    public string? VaterSubFa { get; set; }
    /// <summary>Urspruengliche Sage-Position (<c>FaHierarchyNode.Position</c>). In FullStructure ist
    /// <see cref="BomItem.Position"/> ein rekursiver Anzeige-Pfad — die Sage-Position bleibt hier sichtbar
    /// (Freigabe-Bedingung a).</summary>
    public string? SagePosition { get; set; }
    /// <summary>Blatt-/Struktur-Waise (VaterFA ohne importierten Elternknoten), markiert an die HauptFA gehaengt.</summary>
    public bool IsWaise { get; set; }
    /// <summary>Zwei Geschwister mit gleicher Position unter demselben Eltern-FA — Pfad wurde mit Suffix
    /// eindeutig gemacht und der Fall protokolliert, nie still ueberschrieben (Freigabe-Bedingung b).</summary>
    public bool PositionsKollision { get; set; }
}
```

- [ ] **Step 2: Failing tests schreiben**

`IdealAkeWms.Tests/Repositories/FaHierarchyBomRepositoryTests.cs`:
```csharp
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class FaHierarchyBomRepositoryTests
{
    // Struktur HauptFA 100 (Wurzel SubFA=100, Artnr DEV):
    //   Pos 3  -> Sub-FA 1043421 (Artnr A1, Baugruppe)
    //       Pos 7 -> Sub-FA 1043422 (Artnr A2, Baugruppe)
    //           Pos 2 -> Blatt L1 (Sollmenge 5, Komm. "PG", Lagerplatz "R1")
    //       Pos 2 -> Blatt L2
    //   Pos 5  -> Blatt L3 (Beschaffungsartikel)
    private static List<FaHierarchyNode> Sample() => new()
    {
        N(100, null, 100, pos: null, artnr: "DEV", hauptArtnr: "DEV"),
        N(100, 100, 1043421, pos: 3, artnr: "A1"),
        N(100, 1043421, 1043422, pos: 7, artnr: "A2"),
        N(100, 1043422, 0, pos: 2, artnr: "L1", sollmenge: 5m, komm: "PG", lager: "R1"),
        N(100, 1043421, 0, pos: 2, artnr: "L2"),
        N(100, 100, 0, pos: 5, artnr: "L3", beschaffung: true),
    };

    private static FaHierarchyNode N(int hauptFa, int? vaterFa, int subFa, int? pos, string artnr,
        string? hauptArtnr = null, decimal sollmenge = 1m, string? komm = null, string? lager = null,
        bool beschaffung = false, string? artikelgruppe = null) => new()
    {
        HauptFA = hauptFa, VaterFA = vaterFa, SubFA = subFa, Position = pos, Artnr = artnr,
        HauptArtnr = hauptArtnr ?? "DEV", Bezeichnung1 = "B1-" + artnr, Bezeichnung2 = "B2-" + artnr,
        Sollmenge = sollmenge, Kommissionieren = komm, Hauptlagerplatz = lager,
        Beschaffungsartikel = beschaffung, Artikelgruppe = artikelgruppe ?? "940 - Kleinmaterial",
    };

    private static FaHierarchyBomRepository Sut(List<FaHierarchyNode> nodes)
    {
        var repo = new Mock<IFaHierarchyNodeRepository>();
        repo.Setup(r => r.GetByHauptFaAsync(100)).ReturnsAsync(nodes);
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(nodes);
        return new FaHierarchyBomRepository(repo.Object,
            new FaHierarchyTreeBuilder(NullLogger<FaHierarchyTreeBuilder>.Instance),
            NullLogger<FaHierarchyBomRepository>.Instance);
    }

    [Fact]
    public async Task DirectChildren_OfSubFa_MapsFields_AndFlagsBaugruppe()
    {
        var r = await Sut(Sample()).GetBomItemsAsync(new BomKey("A1", "1043421", "100"), BomScope.DirectChildren);

        r.DataSource.Should().Be(BomDataSources.FaHierarchie);
        r.MengeIstAuftragsmenge.Should().BeTrue();
        r.Items.Should().HaveCount(2);
        var bg = r.Items.OfType<FaHierarchyBomItem>().Single(i => i.Ressourcenummer == "A2");
        bg.IsBaugruppe.Should().BeTrue();
        bg.Position.Should().Be("7");
        bg.SagePosition.Should().Be("7");
        bg.Artikelnummer.Should().Be("A1");          // Kopf = Artikel des Eltern-Sub-FA
        bg.Baugruppe.Should().Be("A1");
        bg.VaterSubFa.Should().Be("1043421");
        bg.Ebene.Should().Be(1);
        var leaf = r.Items.OfType<FaHierarchyBomItem>().Single(i => i.Ressourcenummer == "L2");
        leaf.IsBaugruppe.Should().BeFalse();
        leaf.Beschaffungsartikel.Should().Be("Nein");
        leaf.Bezeichnung1.Should().Be("B1-L2");
        leaf.Artikelgruppe.Should().Be("940 - Kleinmaterial");
    }

    [Fact]
    public async Task DirectChildren_OfRoot_UsesHauptArtnrAsHeader_AndSollmenge()
    {
        var r = await Sut(Sample()).GetBomItemsAsync(new BomKey("DEV", "100", "100"), BomScope.DirectChildren);

        r.Items.Should().HaveCount(2);
        var l3 = r.Items.OfType<FaHierarchyBomItem>().Single(i => i.Ressourcenummer == "L3");
        l3.Artikelnummer.Should().Be("DEV");
        l3.Beschaffungsartikel.Should().Be("Ja");
        l3.Menge.Should().Be(1m);
    }

    [Fact]
    public async Task FullStructure_BuildsRecursivePaths_EveryNodeOnce()
    {
        var r = await Sut(Sample()).GetBomItemsAsync(new BomKey("DEV", "100", "100"), BomScope.FullStructure);

        var items = r.Items.Cast<FaHierarchyBomItem>().ToList();
        items.Should().HaveCount(5); // alle Nicht-Wurzel-Knoten genau einmal
        items.Select(i => i.Position).Should().BeEquivalentTo(new[] { "3", "3.7", "3.7.2", "3.2", "5" });
        items.Single(i => i.Ressourcenummer == "L1").SagePosition.Should().Be("2");   // Sage-Position bleibt sichtbar
        items.Single(i => i.Ressourcenummer == "L1").Ebene.Should().Be(3);
        items.Single(i => i.Ressourcenummer == "L1").VaterSubFa.Should().Be("1043422");
        items.Single(i => i.Ressourcenummer == "L1").Kommissionieren.Should().Be("PG");
        items.Single(i => i.Ressourcenummer == "L1").Hauptlagerplatz.Should().Be("R1");
        items.Single(i => i.Ressourcenummer == "L1").Menge.Should().Be(5m);
        // Baugruppe = Artnr des unmittelbaren Elternknotens
        items.Single(i => i.Ressourcenummer == "L1").Baugruppe.Should().Be("A2");
        items.Single(i => i.Ressourcenummer == "A1").Baugruppe.Should().Be("DEV");
    }

    [Fact]
    public async Task FullStructure_SamePositionUnderDifferentParents_YieldsDistinctPaths()
    {
        // L1 (unter 1043422) und L2 (unter 1043421) tragen beide Sage-Position 2 -> "3.7.2" vs "3.2".
        var r = await Sut(Sample()).GetBomItemsAsync(new BomKey("DEV", "100", "100"), BomScope.FullStructure);

        r.Items.Select(i => i.Position).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task FullStructure_CollisionAmongSiblings_IsMarkedNotOverwritten()
    {
        var nodes = Sample();
        nodes.Add(N(100, 100, 0, pos: 5, artnr: "L4")); // zweites Wurzel-Kind mit Position 5

        var r = await Sut(nodes).GetBomItemsAsync(new BomKey("DEV", "100", "100"), BomScope.FullStructure);

        var items = r.Items.Cast<FaHierarchyBomItem>().ToList();
        items.Should().HaveCount(6);
        items.Select(i => i.Position).Should().OnlyHaveUniqueItems();
        items.Count(i => i.PositionsKollision).Should().Be(1);
        items.Single(i => i.PositionsKollision).Position.Should().Be("5~2");
    }

    [Fact]
    public async Task FullStructure_LeafOrphan_IsAppendedMarked()
    {
        var nodes = Sample();
        nodes.Add(N(100, 999999, 0, pos: 1, artnr: "ORPHAN")); // VaterFA existiert nicht

        var r = await Sut(nodes).GetBomItemsAsync(new BomKey("DEV", "100", "100"), BomScope.FullStructure);

        var orphan = r.Items.Cast<FaHierarchyBomItem>().Single(i => i.Ressourcenummer == "ORPHAN");
        orphan.IsWaise.Should().BeTrue();
        orphan.Position.Should().Be("W1");
        orphan.VaterSubFa.Should().Be("999999");
    }

    [Fact]
    public async Task DirectChildren_Partition_CoversEveryNonRootNodeExactlyOnce()
    {
        // Property (Spec AK 4): Vereinigung aller DirectChildren ueber alle Sub-FAs (inkl. Wurzel) =
        // alle Nicht-Wurzel-Knoten, ohne Doppelung.
        var nodes = Sample();
        var sut = Sut(nodes);
        var parents = nodes.Where(n => n.SubFA != 0).ToList();
        var union = new List<string>();
        foreach (var p in parents)
        {
            var r = await sut.GetBomItemsAsync(new BomKey(p.Artnr!, p.SubFA.ToString(), "100"), BomScope.DirectChildren);
            union.AddRange(r.Items.Select(i => i.Ressourcenummer!));
        }
        union.Should().BeEquivalentTo(nodes.Where(n => n.VaterFA != null).Select(n => n.Artnr));
        union.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task UnknownSubFa_ReturnsEmptyResult_NotException()
    {
        var r = await Sut(Sample()).GetBomItemsAsync(new BomKey("X", "424242", "100"));

        r.Items.Should().BeEmpty();
        r.DataSource.Should().Be(BomDataSources.FaHierarchie);
    }

    [Fact]
    public async Task ReverseLookup_ReturnsParentSubFas_AndSollmengePerParent()
    {
        var nodes = Sample();
        nodes.Add(N(100, 1043421, 0, pos: 9, artnr: "L1", sollmenge: 2m)); // L1 auch unter 1043421

        var sut = Sut(nodes);
        var parents = await sut.GetDeviceArticleNumbersByComponentAsync("L1");
        var menge = await sut.GetComponentMengePerDeviceAsync("L1");

        parents.Should().BeEquivalentTo(new[] { "1043422", "1043421" });
        menge.Should().BeEquivalentTo(new Dictionary<string, decimal> { ["1043422"] = 5m, ["1043421"] = 2m });
    }

    [Fact]
    public async Task WriteMethods_AreNoOps()
    {
        var sut = Sut(Sample());
        await sut.DeleteOrphansAsync(new List<string> { "X" });
        (await sut.GetHeaderHashesAsync(new List<string> { "X" })).Should().BeEmpty();
    }
}
```
Hinweis: Die exakten Parameterlisten von `GetHeaderHashesAsync`/`UpsertBomAsync`/`GetArticleNumbersWithCoatingPartsAsync` stehen in `IBomCacheRepository.cs` (Zeilen 19-42) — Test-Aufruf ggf. an die Parameter anpassen; erwartetes Verhalten: leer bzw. No-op.

- [ ] **Step 3: Tests laufen lassen → FAIL (Klasse fehlt)**

Run: `dotnet test IdealAkeWms.Tests --filter "FullyQualifiedName~FaHierarchyBomRepositoryTests"` → Compile-Fehler erwartet.

- [ ] **Step 4: Repository implementieren**

`IdealAkeWms/Data/Repositories/FaHierarchyBomRepository.cs`:
```csharp
using System.Globalization;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// BOM-Bridge (Spec 2026-09-08): zweite Implementierung von <see cref="IBomRepository"/> UND
/// <see cref="IBomCacheRepository"/>, die im hierarchischen Modus (IDEAL) die Stueckliste direkt aus der
/// lokalen FA-Struktur (<see cref="FaHierarchyNode"/>) liest — KEIN Zugriff auf den persistenten AKE-BOM-Cache,
/// kein MemoryCache-Decorator (die Struktur ist lokal und ueber CachedFaHierarchyNodeRepository 5 min gecacht).
/// Mengen sind Auftragsgesamtmengen (<c>MengeIstAuftragsmenge = true</c>).
/// Feld-Mapping laut Anhang sage-views-ideal: Kopf <c>Artikelnummer</c> = Artikel des Elternknotens,
/// <c>Ressourcenummer</c> = <c>Artnr</c>, <c>Baugruppe</c> = Artnr des unmittelbaren Elternknotens.
/// </summary>
public sealed class FaHierarchyBomRepository : IBomRepository, IBomCacheRepository
{
    private readonly IFaHierarchyNodeRepository _nodes;
    private readonly FaHierarchyTreeBuilder _treeBuilder;
    private readonly ILogger<FaHierarchyBomRepository> _logger;

    public FaHierarchyBomRepository(
        IFaHierarchyNodeRepository nodes,
        FaHierarchyTreeBuilder treeBuilder,
        ILogger<FaHierarchyBomRepository> logger)
    {
        _nodes = nodes;
        _treeBuilder = treeBuilder;
        _logger = logger;
    }

    private static BomQueryResult Empty() =>
        new(new List<BomItem>(), BomDataSources.FaHierarchie, MengeIstAuftragsmenge: true);

    // ---------------------------------------------------------------- IBomRepository

    public async Task<BomQueryResult> GetBomItemsAsync(BomKey key, BomScope scope = BomScope.DirectChildren)
    {
        if (!int.TryParse(key.SubOrderNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var subFa))
        {
            _logger.LogWarning("FaHierarchyBom: SubOrderNumber '{Sub}' ist keine Sub-FA-Nummer — leere Stueckliste.", key.SubOrderNumber);
            return Empty();
        }

        var group = await LoadGroupAsync(key.OrderNumber, subFa);
        var self = group.FirstOrDefault(n => n.SubFA == subFa);
        if (self == null)
        {
            _logger.LogWarning("FaHierarchyBom: Sub-FA {SubFa} nicht in der Struktur (HauptFA {HauptFa}) — leere Stueckliste.", subFa, key.OrderNumber);
            return Empty();
        }

        // FullStructure ist nur an der Wurzel definiert (Fachliche Anforderung 6); an einem Sub-FA
        // faellt der Aufruf bewusst auf DirectChildren zurueck.
        var items = scope == BomScope.FullStructure && self.VaterFA == null
            ? BuildFullStructure(group, self)
            : BuildDirectChildren(group, self);

        return new BomQueryResult(items, BomDataSources.FaHierarchie, MengeIstAuftragsmenge: true);
    }

    /// <summary>HauptFA aus dem Key (gecachter Gruppen-Lookup); ohne HauptFA Fallback ueber GetAllAsync.</summary>
    private async Task<List<FaHierarchyNode>> LoadGroupAsync(string? orderNumber, int subFa)
    {
        if (int.TryParse(orderNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hauptFa))
            return await _nodes.GetByHauptFaAsync(hauptFa);

        var all = await _nodes.GetAllAsync();
        var owner = all.FirstOrDefault(n => n.SubFA == subFa);
        return owner == null ? new List<FaHierarchyNode>() : all.Where(n => n.HauptFA == owner.HauptFA).ToList();
    }

    private static List<BomItem> BuildDirectChildren(List<FaHierarchyNode> group, FaHierarchyNode parent)
    {
        var children = group
            .Where(n => n.VaterFA == parent.SubFA)
            .OrderBy(n => n.Position ?? int.MaxValue)
            .ToList();

        var used = new HashSet<string>(StringComparer.Ordinal);
        var items = new List<BomItem>(children.Count);
        foreach (var child in children)
        {
            var segment = PositionSegment(child);
            var (position, collided) = Unique(segment, used);
            items.Add(Map(child, parent, position, ebene: 1, isWaise: false, collided));
        }
        return items;
    }

    private List<BomItem> BuildFullStructure(List<FaHierarchyNode> group, FaHierarchyNode root)
    {
        var items = new List<BomItem>();
        var structures = _treeBuilder.Build(group, Array.Empty<FaHierarchyOrderInfo>(), FaHierarchyTreeBuilder.DefaultDepthCap);

        var main = structures.FirstOrDefault(s => !s.IsOrphan && s.Roots.Count > 0 && s.Roots[0].Node.SubFA == root.SubFA);
        if (main != null)
        {
            if (main.HasError)
                _logger.LogWarning("FaHierarchyBom: Struktur HauptFA {HauptFa} unvollstaendig ({Reason}) — Stueckliste zeigt den aufgebauten Teil.", root.HauptFA, main.ErrorMessage);
            WalkChildren(main.Roots[0], parentPath: null, ebene: 1, isWaise: false, items);
        }

        // Waisen (VaterFA ohne importierten Elternknoten) markiert anhaengen: eigener Pfad-Praefix "W<n>",
        // damit die Punktnotation der View weiterhin gueltige Eltern hat.
        var w = 0;
        foreach (var orphanStructure in structures.Where(s => s.IsOrphan && s.Roots.Count > 0))
        {
            w++;
            var pseudoRoot = orphanStructure.Roots[0];
            var path = "W" + w.ToString(CultureInfo.InvariantCulture);
            var parentStub = new FaHierarchyNode { HauptFA = root.HauptFA, SubFA = pseudoRoot.Node.VaterFA ?? 0, Artnr = null, HauptArtnr = root.HauptArtnr };
            items.Add(Map(pseudoRoot.Node, parentStub, path, ebene: 1, isWaise: true, collided: false));
            WalkChildren(pseudoRoot, parentPath: path, ebene: 2, isWaise: true, items);
        }
        return items;
    }

    private void WalkChildren(FaHierarchyTreeNode parent, string? parentPath, int ebene, bool isWaise, List<BomItem> items)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var child in parent.Children.OrderBy(c => c.Node.Position ?? int.MaxValue))
        {
            var segment = PositionSegment(child.Node);
            var (uniqueSegment, collided) = Unique(segment, used);
            if (collided)
                _logger.LogWarning("FaHierarchyBom: Positions-Kollision unter Sub-FA {Parent} (Position {Pos}) — Pfad eindeutig gemacht.", parent.Node.SubFA, segment);
            var path = parentPath == null ? uniqueSegment : parentPath + "." + uniqueSegment;
            items.Add(Map(child.Node, parent.Node, path, ebene, isWaise, collided));
            WalkChildren(child, path, ebene + 1, isWaise, items);
        }
    }

    private static string PositionSegment(FaHierarchyNode node) =>
        node.Position.HasValue ? node.Position.Value.ToString(CultureInfo.InvariantCulture) : "0";

    /// <summary>Freigabe-Bedingung (b): gleiche Position unter demselben Eltern-FA wird mit Suffix "~n"
    /// eindeutig gemacht und als Kollision markiert — nie still ueberschrieben.</summary>
    private static (string Segment, bool Collided) Unique(string segment, HashSet<string> used)
    {
        if (used.Add(segment)) return (segment, false);
        var n = 2;
        while (!used.Add(segment + "~" + n.ToString(CultureInfo.InvariantCulture))) n++;
        return (segment + "~" + n.ToString(CultureInfo.InvariantCulture), true);
    }

    private static FaHierarchyBomItem Map(FaHierarchyNode node, FaHierarchyNode parent, string position, int ebene, bool isWaise, bool collided)
    {
        var parentArticle = parent.Artnr ?? parent.HauptArtnr ?? string.Empty;
        return new FaHierarchyBomItem
        {
            Artikelnummer = parentArticle,
            Position = position,
            Baugruppe = parentArticle,
            Ressourcenummer = node.Artnr,
            Bezeichnung1 = node.Bezeichnung1,
            Bezeichnung2 = node.Bezeichnung2,
            Menge = node.Sollmenge,
            Beschaffungsartikel = node.Beschaffungsartikel ? "Ja" : "Nein",
            Artikelgruppe = node.Artikelgruppe,
            IsBaugruppe = node.SubFA != 0,
            Kommissionieren = node.Kommissionieren,
            Hauptlagerplatz = node.Hauptlagerplatz,
            Ebene = ebene,
            VaterSubFa = (node.VaterFA ?? parent.SubFA).ToString(CultureInfo.InvariantCulture),
            SagePosition = node.Position?.ToString(CultureInfo.InvariantCulture),
            IsWaise = isWaise,
            PositionsKollision = collided,
        };
    }

    // ---------------------------------------------------------------- IBomCacheRepository

    public async Task<BomQueryResult?> GetByArticleNumberAsync(BomKey key) =>
        await GetBomItemsAsync(key, BomScope.DirectChildren);

    /// <summary>Artikelinfo (Spec Design G): "Geraete" sind hier die SubOrderNumbers der Eltern-Sub-FAs,
    /// deren direktes Kind das Bauteil ist (Partition DirectChildren, jeder Knoten genau einmal).</summary>
    public async Task<List<string>> GetDeviceArticleNumbersByComponentAsync(string componentArticleNumber)
    {
        if (string.IsNullOrWhiteSpace(componentArticleNumber)) return new List<string>();
        var all = await _nodes.GetAllAsync();
        return all
            .Where(n => n.VaterFA.HasValue && string.Equals(n.Artnr, componentArticleNumber, StringComparison.Ordinal))
            .Select(n => n.VaterFA!.Value.ToString(CultureInfo.InvariantCulture))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public async Task<Dictionary<string, decimal>> GetComponentMengePerDeviceAsync(string componentArticleNumber)
    {
        if (string.IsNullOrWhiteSpace(componentArticleNumber)) return new Dictionary<string, decimal>();
        var all = await _nodes.GetAllAsync();
        return all
            .Where(n => n.VaterFA.HasValue && string.Equals(n.Artnr, componentArticleNumber, StringComparison.Ordinal))
            .GroupBy(n => n.VaterFA!.Value.ToString(CultureInfo.InvariantCulture), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Sum(n => n.Sollmenge), StringComparer.Ordinal);
    }

    // Sync-/Cache-interne Methoden: werden durch die Klasse-D-Gates (Task 4) im hierarchischen Modus nie
    // erreicht — hier nur Sicherheitsnetz: leer bzw. No-op mit Log, nie Exception.
    // (Signaturen 1:1 aus IBomCacheRepository.cs Zeilen 19-42 uebernehmen.)
    public Task<HashSet<string>> GetArticleNumbersWithCoatingPartsAsync(/* Parameter wie Interface */)
        => Task.FromResult(new HashSet<string>(StringComparer.Ordinal));
    public Task<Dictionary<string, (string Hash, DateTime CachedAt)>> GetHeaderHashesAsync(/* Parameter wie Interface */)
        => Task.FromResult(new Dictionary<string, (string Hash, DateTime CachedAt)>());
    public Task UpsertBomAsync(/* Parameter wie Interface */)
    {
        _logger.LogWarning("FaHierarchyBom: UpsertBomAsync im hierarchischen Modus ignoriert (kein BOM-Cache).");
        return Task.CompletedTask;
    }
    public Task DeleteOrphansAsync(List<string> currentArticleNumbers)
    {
        _logger.LogWarning("FaHierarchyBom: DeleteOrphansAsync im hierarchischen Modus ignoriert (kein BOM-Cache).");
        return Task.CompletedTask;
    }
}
```
Die drei `/* Parameter wie Interface */`-Stellen: Parameterliste **wortgleich** aus `IBomCacheRepository.cs` uebernehmen (sonst kompiliert die Klasse nicht); die Namen der Parameter sind egal, sie werden nicht verwendet.

- [ ] **Step 5: Tests laufen lassen → PASS**

Run: `dotnet test IdealAkeWms.Tests --filter "FullyQualifiedName~FaHierarchyBomRepositoryTests"` → 10 PASS.

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Models/FaHierarchyBomItem.cs IdealAkeWms/Data/Repositories/FaHierarchyBomRepository.cs IdealAkeWms.Tests/Repositories/FaHierarchyBomRepositoryTests.cs
git commit -m "wip: bom-bridge Task 2 — FaHierarchyBomRepository (DirectChildren/FullStructure, rekursiver Pfad, Kollision, Waisen, Reverse-Lookup)"
```

---

### Task 3: Master-Weiche, Guard entfernen, Controller/Builder auf hierarchische Zeilen

**Files:**
- Create: `IdealAkeWms/Data/Repositories/BomRepositoryMasterSwitch.cs`
- Delete: `IdealAkeWms/Data/Repositories/HierarchicalBomGuardRepository.cs`, `IdealAkeWms.Tests/Repositories/HierarchicalBomGuardRepositoryTests.cs`
- Modify: `IdealAkeWms/Program.cs:91-98`, `IdealAkeWms/Models/ViewModels/BomViewModels.cs` (`BomDataSources.HierarchicalUnavailable` entfernen; `BomItemViewModel` + `BomViewModel` erweitern), `IdealAkeWms/Controllers/PickingController.cs` (Bom-Mapping Zeile 340-411, PrintBom 575-590), `IdealAkeWms/Services/ReadOnlyBomBuilder.cs` (83-108), `IdealAkeWms/Views/Picking/Bom.cshtml` (Hinweis-Zweig Zeile 41-61 + `else {`/`}` entfernen; Print-Button-Gate aus U1 anpassen; Kopfzeilen-Badge Zeile 19-22)
- Test: `IdealAkeWms.Tests/Repositories/BomRepositoryMasterSwitchTests.cs` (neu), `IdealAkeWms.Tests/Controllers/PickingControllerTests.cs` (+1), `IdealAkeWms.Tests/Services/ReadOnlyBomBuilderTests.cs` (+1)

**Interfaces:**
- Consumes: Task 1 + Task 2.
- Produces: `BomRepositoryMasterSwitch(IServiceSettingRepository settings, Func<IBomRepository> hierarchicalBom, Func<IBomCacheRepository> hierarchicalCache, Func<IBomRepository> flatBom, Func<IBomCacheRepository> flatCache) : IBomRepository, IBomCacheRepository`; `BomItemViewModel { string? Kommissionieren; string? Hauptlagerplatz; int Ebene; string? VaterSubFa; string? SagePosition; bool IsWaise; bool PositionsKollision; }`; `BomViewModel { bool Hierarchical; bool FullStructure; }`.

- [ ] **Step 1: Failing Switch-Tests**

`IdealAkeWms.Tests/Repositories/BomRepositoryMasterSwitchTests.cs`:
```csharp
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services.HierarchischeStruktur;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class BomRepositoryMasterSwitchTests
{
    private readonly Mock<IBomRepository> _hierBom = new();
    private readonly Mock<IBomCacheRepository> _hierCache = new();
    private readonly Mock<IBomRepository> _flatBom = new();
    private readonly Mock<IBomCacheRepository> _flatCache = new();

    private BomRepositoryMasterSwitch Sut(string? master)
    {
        var settings = new Mock<IServiceSettingRepository>();
        settings.Setup(s => s.GetValueAsync(HierarchischeStrukturKeys.Master)).ReturnsAsync(master);
        return new BomRepositoryMasterSwitch(settings.Object,
            () => _hierBom.Object, () => _hierCache.Object, () => _flatBom.Object, () => _flatCache.Object);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    public async Task MasterTrue_RoutesToHierarchical_NeverTouchesFlat(string master)
    {
        var expected = new BomQueryResult(new List<BomItem>(), BomDataSources.FaHierarchie, true);
        _hierBom.Setup(r => r.GetBomItemsAsync(It.IsAny<BomKey>(), BomScope.FullStructure)).ReturnsAsync(expected);
        _hierCache.Setup(r => r.GetDeviceArticleNumbersByComponentAsync("C")).ReturnsAsync(new List<string> { "1" });

        var sut = Sut(master);
        (await sut.GetBomItemsAsync(new BomKey("A", "1", "1"), BomScope.FullStructure)).Should().BeSameAs(expected);
        (await sut.GetDeviceArticleNumbersByComponentAsync("C")).Should().ContainSingle();
        _flatBom.VerifyNoOtherCalls();
        _flatCache.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public async Task MasterFalseOrMissing_RoutesToFlat_NeverTouchesHierarchical(string? master)
    {
        var expected = new BomQueryResult(new List<BomItem>(), BomDataSources.Sage);
        _flatBom.Setup(r => r.GetBomItemsAsync(It.IsAny<BomKey>(), It.IsAny<BomScope>())).ReturnsAsync(expected);
        _flatCache.Setup(r => r.GetByArticleNumberAsync(It.IsAny<BomKey>())).ReturnsAsync(expected);

        var sut = Sut(master);
        (await sut.GetBomItemsAsync(new BomKey("A"))).Should().BeSameAs(expected);
        (await sut.GetByArticleNumberAsync(new BomKey("A"))).Should().BeSameAs(expected);
        _hierBom.VerifyNoOtherCalls();
        _hierCache.VerifyNoOtherCalls();
    }
}
```

- [ ] **Step 2: Switch implementieren**

`IdealAkeWms/Data/Repositories/BomRepositoryMasterSwitch.cs`:
```csharp
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services.HierarchischeStruktur;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// BOM-Bridge-Weiche (Spec Design A): entscheidet PRO AUFRUF am Master <c>ProduktionsauftragHierarchisch</c>,
/// ob die FA-Hierarchie (<see cref="FaHierarchyBomRepository"/>) oder der unveraenderte AKE-Pfad
/// (<see cref="CachedBomRepository"/> → <see cref="BomRepository"/> → <see cref="BomCacheRepository"/>) bedient.
/// Deckt BEIDE Interfaces ab — auch die bisher ungeschuetzte <see cref="IBomCacheRepository"/>-Registrierung.
/// Die Ziele werden per Delegate LAZY aufgeloest: <see cref="BomRepository"/> haengt selbst an
/// <see cref="IBomCacheRepository"/> (= diese Weiche); eine eager Konstruktion waere ein DI-Zyklus.
/// Nachfolger des Minimal-Guards <c>HierarchicalBomGuardRepository</c> (entfernt).
/// </summary>
public sealed class BomRepositoryMasterSwitch : IBomRepository, IBomCacheRepository
{
    private readonly IServiceSettingRepository _settings;
    private readonly Func<IBomRepository> _hierarchicalBom;
    private readonly Func<IBomCacheRepository> _hierarchicalCache;
    private readonly Func<IBomRepository> _flatBom;
    private readonly Func<IBomCacheRepository> _flatCache;

    public BomRepositoryMasterSwitch(
        IServiceSettingRepository settings,
        Func<IBomRepository> hierarchicalBom,
        Func<IBomCacheRepository> hierarchicalCache,
        Func<IBomRepository> flatBom,
        Func<IBomCacheRepository> flatCache)
    {
        _settings = settings;
        _hierarchicalBom = hierarchicalBom;
        _hierarchicalCache = hierarchicalCache;
        _flatBom = flatBom;
        _flatCache = flatCache;
    }

    private async Task<bool> IsHierarchicalAsync()
    {
        var master = await _settings.GetValueAsync(HierarchischeStrukturKeys.Master);
        return string.Equals(master, "true", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IBomRepository> BomAsync() => await IsHierarchicalAsync() ? _hierarchicalBom() : _flatBom();
    private async Task<IBomCacheRepository> CacheAsync() => await IsHierarchicalAsync() ? _hierarchicalCache() : _flatCache();

    public async Task<BomQueryResult> GetBomItemsAsync(BomKey key, BomScope scope = BomScope.DirectChildren)
        => await (await BomAsync()).GetBomItemsAsync(key, scope);

    public async Task<BomQueryResult?> GetByArticleNumberAsync(BomKey key)
        => await (await CacheAsync()).GetByArticleNumberAsync(key);

    public async Task<List<string>> GetDeviceArticleNumbersByComponentAsync(string componentArticleNumber)
        => await (await CacheAsync()).GetDeviceArticleNumbersByComponentAsync(componentArticleNumber);

    public async Task<Dictionary<string, decimal>> GetComponentMengePerDeviceAsync(string componentArticleNumber)
        => await (await CacheAsync()).GetComponentMengePerDeviceAsync(componentArticleNumber);

    public async Task DeleteOrphansAsync(List<string> currentArticleNumbers)
        => await (await CacheAsync()).DeleteOrphansAsync(currentArticleNumbers);

    // GetArticleNumbersWithCoatingPartsAsync / GetHeaderHashesAsync / UpsertBomAsync:
    // Parameterlisten wortgleich aus IBomCacheRepository.cs uebernehmen und identisch weiterleiten:
    //   => await (await CacheAsync()).<Methode>(<alle Parameter>);
}
```

- [ ] **Step 3: DI-Registrierung ersetzen** (`Program.cs` Zeile 91-98 komplett ersetzen)

```csharp
builder.Services.AddScoped<BomCacheRepository>();
builder.Services.AddScoped<BomRepository>();
builder.Services.AddScoped<CachedBomRepository>();
builder.Services.AddScoped<FaHierarchyBomRepository>();
// BOM-Bridge (Spec 2026-09-08): EINE Weiche fuer beide Interfaces, entscheidet pro Aufruf am Master.
// Lazy-Delegates, weil BomRepository selbst IBomCacheRepository (= die Weiche) injiziert — eager waere ein Zyklus.
builder.Services.AddScoped<BomRepositoryMasterSwitch>(sp => new BomRepositoryMasterSwitch(
    sp.GetRequiredService<IServiceSettingRepository>(),
    hierarchicalBom: () => sp.GetRequiredService<FaHierarchyBomRepository>(),
    hierarchicalCache: () => sp.GetRequiredService<FaHierarchyBomRepository>(),
    flatBom: () => sp.GetRequiredService<CachedBomRepository>(),
    flatCache: () => sp.GetRequiredService<BomCacheRepository>()));
builder.Services.AddScoped<IBomRepository>(sp => sp.GetRequiredService<BomRepositoryMasterSwitch>());
builder.Services.AddScoped<IBomCacheRepository>(sp => sp.GetRequiredService<BomRepositoryMasterSwitch>());
```
`FaHierarchyTreeBuilder` ist bereits registriert (`Program.cs:143`), `IFaHierarchyNodeRepository` ebenfalls (`:102`).

- [ ] **Step 4: Guard + Konstante entfernen**

`git rm IdealAkeWms/Data/Repositories/HierarchicalBomGuardRepository.cs IdealAkeWms.Tests/Repositories/HierarchicalBomGuardRepositoryTests.cs`. In `BomDataSources` die Konstante `HierarchicalUnavailable` (Zeile 18 + Kommentar) entfernen. Build zeigt danach jede verbliebene Verwendung (Bom.cshtml Zeile 19-22, 41-61 und das U1-Gate um den Druck-Button) — siehe Step 6.

- [ ] **Step 5: ViewModels erweitern** (`BomViewModels.cs`)

In `BomItemViewModel` nach `IsBaugruppe` (Zeile 58):
```csharp
    // BOM-Bridge (hierarchisch, nur befuellt wenn die Zeile ein FaHierarchyBomItem ist)
    public string? Kommissionieren { get; set; }
    public string? Hauptlagerplatz { get; set; }
    public int Ebene { get; set; }
    public string? VaterSubFa { get; set; }
    public string? SagePosition { get; set; }
    public bool IsWaise { get; set; }
    public bool PositionsKollision { get; set; }
```
In `BomViewModel` nach `RecursiveFilterSearch` (Zeile 87):
```csharp
    /// <summary>Quelle ist die FA-Hierarchie (DataSource FA-HIERARCHIE): hierarchische Spalten rendern.</summary>
    public bool Hierarchical { get; set; }
    /// <summary>Vollstruktur (HauptFA): zusaetzlich Ebene/Vater-Sub-FA rendern.</summary>
    public bool FullStructure { get; set; }
```

- [ ] **Step 6: Controller/Builder-Mapping auf hierarchische Zeilen**

`PickingController.cs` im `viewItems`-Lambda (Zeile 388-408) das `new BomItemViewModel { … }` ergaenzen:
```csharp
                IsBaugruppe = bom is FaHierarchyBomItem fh
                    ? fh.IsBaugruppe
                    : !string.IsNullOrEmpty(bom.Ressourcenummer) && baugruppen.Contains(bom.Ressourcenummer),
                Kommissionieren = (bom as FaHierarchyBomItem)?.Kommissionieren,
                Hauptlagerplatz = (bom as FaHierarchyBomItem)?.Hauptlagerplatz,
                Ebene = (bom as FaHierarchyBomItem)?.Ebene ?? 0,
                VaterSubFa = (bom as FaHierarchyBomItem)?.VaterSubFa,
                SagePosition = (bom as FaHierarchyBomItem)?.SagePosition,
                IsWaise = (bom as FaHierarchyBomItem)?.IsWaise ?? false,
                PositionsKollision = (bom as FaHierarchyBomItem)?.PositionsKollision ?? false,
```
(die bisherige `IsBaugruppe = …`-Zeile 402 ersetzen). Beim `BomViewModel` (Zeile 424-439) ergaenzen:
```csharp
            Hierarchical = bomResult.DataSource == BomDataSources.FaHierarchie,
            FullStructure = bomResult.DataSource == BomDataSources.FaHierarchie && BomScopes.ForOrder(order) == BomScope.FullStructure,
```
`PrintBom` (Zeile 586): `IsBaugruppe = bom is FaHierarchyBomItem fh ? fh.IsBaugruppe : (!string.IsNullOrEmpty(bom.Ressourcenummer) && baugruppen.Contains(bom.Ressourcenummer)),`.
`ReadOnlyBomBuilder.cs` (Zeile 90-105): dieselben Zuweisungen wie im PickingController-Lambda; im `return new BomViewModel` (Zeile 121-134) dieselben zwei Flags `Hierarchical`/`FullStructure`.

`Views/Picking/Bom.cshtml`:
- Zeile 19-22 (`else if (Model.DataSource == BomDataSources.HierarchicalUnavailable)`-Badge) ersetzen durch:
  ```cshtml
            else if (Model.DataSource == BomDataSources.FaHierarchie)
            {
                <span class="badge bg-info text-dark ms-2 fs-6 fw-normal align-middle" title="Quelle: FA-Struktur (Sollmengen)">FA-Struktur</span>
            }
  ```
- Das U1-Gate `@if (Model.DataSource != BomDataSources.HierarchicalUnavailable) { … }` um den Druck-Button: Gate entfernen, Button wieder unbedingt rendern (Hinweisseite gibt es nicht mehr).
- Zeile 41-62 (`@if (Model.DataSource == BomDataSources.HierarchicalUnavailable) { … } else {`) entfernen **und** die zugehoerige schliessende Klammer `}` unmittelbar vor dem Kommentar `@* Toolbar-Host …` (Zeile 152) entfernen. Der JS-Fruehabbruch `if (!document.getElementById('bomTable')) return;` bleibt (generisch, harmlos).

- [ ] **Step 7: Tests**

`PickingControllerTests.cs` — neuer Test (nutzt `SetupForBom` aus dem U1-Nachtrag):
```csharp
    [Fact]
    public async Task Bom_Hierarchical_DoesNotMultiplyQuantity_AndFlagsHierarchical()
    {
        var order = OrderH("1035235", "1043421");
        order.Id = 101; order.ArticleNumber = "A1"; order.Quantity = 4m;
        var items = new List<BomItem> { new FaHierarchyBomItem { Ressourcenummer = "L1", Position = "2", Menge = 5m, Kommissionieren = "PG" } };
        var ctrl = SetupForBom(order, new BomQueryResult(items, BomDataSources.FaHierarchie, MengeIstAuftragsmenge: true));

        var vm = (await ctrl.Bom(101, null)).Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<BomViewModel>().Subject;

        vm.Hierarchical.Should().BeTrue();
        vm.FullStructure.Should().BeFalse();            // Sub-FA != HauptFA
        vm.Items.Single().Menge.Should().Be(5m);        // keine × Quantity
        vm.Items.Single().Kommissionieren.Should().Be("PG");
    }
```
`ReadOnlyBomBuilderTests.cs`: analoger Test mit `MengeIstAuftragsmenge: true` → `Menge` unveraendert (Setup-Muster der Datei uebernehmen, `GetBomItemsAsync(It.IsAny<BomKey>(), It.IsAny<BomScope>())`).

- [ ] **Step 8: Build + Tests + Commit**

Run: `dotnet build IdealAkeWms.slnx` (0 Fehler; keine Referenz auf `HierarchicalUnavailable`/`HierarchicalBomGuardRepository` mehr: `grep -rn "HierarchicalUnavailable\|HierarchicalBomGuardRepository" IdealAkeWms IdealAkeWms.Tests` → leer). `dotnet test IdealAkeWms.Tests` gruen.
```bash
git add -A IdealAkeWms IdealAkeWms.Tests
git commit -m "wip: bom-bridge Task 3 — Master-Weiche fuer beide Interfaces, Guard entfernt, hierarchische Zeilen in Picking/Vorbau"
```
(`git add -A` nur auf die zwei Projektordner — `publish.zip` liegt im Root und bleibt unstaged.)

---

### Task 4: Klasse-D-Gates im Service (Coating, WorkStep, BomCache ×2) — parallel zu Task 2 moeglich

**Files:**
- Create: `IDEALAKEWMSService/Common/IHierarchicalModeReader.cs`
- Modify: `IDEALAKEWMSService/Services/CoatingDetectionService.cs` (Ctor Zeile 20-28, Gate nach Zeile 35), `FaWorkStepDetectionService.cs` (Ctor 22-31, Gate nach Zeile 35), `BomCacheSyncService.cs` (Ctor Zeile 24-31, Gate nach Zeile 38 und am Anfang von `SyncSpecificArticleNumbersAsync` Zeile 170-176), Service-`Program.cs` (Registrierung)
- Test: `IDEALAKEWMSService.Tests/Helpers/StubHierarchicalModeReader.cs` (neu), `IDEALAKEWMSService.Tests/Services/HierarchicalModeGateTests.cs` (neu); anpassen: `CoatingDetectionServiceTests.cs`, `FaWorkStepDetectionServiceTests.cs`, `BomCacheSyncServiceLifecycleTests.cs` und jede weitere Stelle mit `new CoatingDetectionService(`/`new FaWorkStepDetectionService(`/`new BomCacheSyncService(` (per `grep -rn` in `IDEALAKEWMSService.Tests`).

**Interfaces:**
- Produces: `IHierarchicalModeReader { Task<bool> IsHierarchicalAsync(CancellationToken ct); }`, `HierarchicalModeReader(IConfiguration)`; Konstruktoren: `CoatingDetectionService(IConfiguration, IHierarchicalModeReader, ILogger<…>, ISyncLogger)`, `FaWorkStepDetectionService(ApplicationDbContext, IHierarchicalModeReader, ILogger<…>, ISyncLogger)`, `BomCacheSyncService(IConfiguration, IHierarchicalModeReader, ILogger<…>, ISyncLogger)`.

- [ ] **Step 1: Reader anlegen**

`IDEALAKEWMSService/Common/IHierarchicalModeReader.cs`:
```csharp
using Microsoft.Extensions.Configuration;

namespace IDEALAKEWMSService.Common;

/// <summary>
/// Master-Read <c>ProduktionsauftragHierarchisch</c> fuer die Klasse-D-Gates (BOM-Bridge, Spec Design H).
/// Kapselt <see cref="ServiceSettings.GetBoolSafeAsync"/> (DB-first, ADR 0008), damit die Gates ohne
/// SQL Server testbar sind (Tests injizieren einen Stub). Safe-Reader: DB-Hickup → false (= AKE-Verhalten).
/// </summary>
public interface IHierarchicalModeReader
{
    Task<bool> IsHierarchicalAsync(CancellationToken ct);
}

public sealed class HierarchicalModeReader : IHierarchicalModeReader
{
    private readonly IConfiguration _configuration;
    public HierarchicalModeReader(IConfiguration configuration) => _configuration = configuration;

    public Task<bool> IsHierarchicalAsync(CancellationToken ct) =>
        ServiceSettings.GetBoolSafeAsync(_configuration, "ProduktionsauftragHierarchisch", false, ct);
}
```
Service-`Program.cs`: neben den bestehenden Service-Registrierungen (`grep -n "ICoatingDetectionService" IDEALAKEWMSService/Program.cs`) ergaenzen: `services.AddSingleton<IHierarchicalModeReader, HierarchicalModeReader>();` (gleiche Registrierungs-Syntax wie die Nachbarzeilen verwenden).

- [ ] **Step 2: Test-Stub + failing Gate-Tests**

`IDEALAKEWMSService.Tests/Helpers/StubHierarchicalModeReader.cs`:
```csharp
using IDEALAKEWMSService.Common;

namespace IDEALAKEWMSService.Tests.Helpers;

public sealed class StubHierarchicalModeReader : IHierarchicalModeReader
{
    private readonly bool _value;
    public StubHierarchicalModeReader(bool value) => _value = value;
    public Task<bool> IsHierarchicalAsync(CancellationToken ct) => Task.FromResult(_value);
}
```
`IDEALAKEWMSService.Tests/Services/HierarchicalModeGateTests.cs`:
```csharp
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>BOM-Bridge Design H: bei Master true fuehrt keiner der vier Einstiege einen Durchlauf aus —
/// kein DB-Zugriff (die leere Konfiguration hat keine Connection-Strings und wuerde sonst werfen).</summary>
public class HierarchicalModeGateTests
{
    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    [Fact]
    public async Task CoatingDetection_SkipsWithoutDbAccess_WhenHierarchical()
    {
        var log = new FakeSyncLogger();
        var svc = new CoatingDetectionService(EmptyConfig(), new StubHierarchicalModeReader(true),
            NullLogger<CoatingDetectionService>.Instance, log);

        var result = await svc.DetectAndUpdateCoatingFlagsAsync(false, null, CancellationToken.None);

        result.Inserted.Should().Be(0);
        log.Runs.Should().ContainSingle();
        log.Runs[0].FinishedSuccess.Should().BeTrue();
        log.Runs[0].FinalCounts!["uebersprungen_hierarchisch"].Should().Be(1);
    }

    [Fact]
    public async Task FaWorkStepDetection_SkipsWithoutTouchingDb_WhenHierarchical()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.WorkSteps.Add(new WorkStep { Code = "VL", Name = "VL", SearchString = "luefter", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.ProductionOrders.Add(new ProductionOrder { OrderNumber = "FA-1", ArticleNumber = "ART-1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.CachedBomHeaders.Add(new CachedBomHeader { Artikelnummer = "ART-1", Source = "SAGE", ContentHash = "h", CachedAt = DateTime.UtcNow, ItemCount = 1,
            Items = new List<CachedBomItem> { new() { Bezeichnung1 = "Axialluefter", SortOrder = 0 } } });
        await ctx.SaveChangesAsync();
        var log = new FakeSyncLogger();
        var svc = new FaWorkStepDetectionService(ctx, new StubHierarchicalModeReader(true), NullLogger<FaWorkStepDetectionService>.Instance, log);

        var result = await svc.DetectAsync(dryRun: false);

        result.Inserted.Should().Be(0);
        (await ctx.FaWorkSteps.CountAsync()).Should().Be(0);   // haette sonst 1 angelegt
        log.Runs[0].FinishedSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BomCacheSync_BothEntries_SkipWithoutDbAccess_WhenHierarchical()
    {
        var log = new FakeSyncLogger();
        var svc = new BomCacheSyncService(EmptyConfig(), new StubHierarchicalModeReader(true), NullLogger<BomCacheSyncService>.Instance, log);

        var full = await svc.SyncBomCacheAsync(dryRun: false, ct: CancellationToken.None);
        var specific = await svc.SyncSpecificArticleNumbersAsync(new List<string> { "ART-1" }, dryRun: false, ct: CancellationToken.None);

        full.Inserted.Should().Be(0);
        specific.Inserted.Should().Be(0);
        log.Runs.Should().ContainSingle();           // nur SyncBomCacheAsync oeffnet einen Run
        log.Runs[0].FinishedSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Flat_StillReachesDb_AndFailsOnMissingConnection()
    {
        // Regression: Master false -> Verhalten wie vor der Bridge (Lifecycle-Test-Erwartung).
        var svc = new BomCacheSyncService(EmptyConfig(), new StubHierarchicalModeReader(false), NullLogger<BomCacheSyncService>.Instance, new FakeSyncLogger());
        var act = async () => await svc.SyncBomCacheAsync(dryRun: false, ct: CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }
}
```

- [ ] **Step 3: Tests laufen lassen → FAIL (Konstruktoren)**

Run: `dotnet test IDEALAKEWMSService.Tests --filter "FullyQualifiedName~HierarchicalModeGateTests"` → Compile-Fehler.

- [ ] **Step 4: Gates einbauen**

Konstruktoren (Parameter **vor** `ISyncLogger`): Feld `private readonly IHierarchicalModeReader _hierarchicalMode;` + Zuweisung.

`CoatingDetectionService.DetectAndUpdateCoatingFlagsAsync` direkt nach `await using var run = …` (Zeile 35) und vor `int updatedTrue …`:
```csharp
        if (await _hierarchicalMode.IsHierarchicalAsync(ct))
        {
            _logger.LogInformation("CoatingDetection: uebersprungen — hierarchischer Modus (BOM-Bridge, Klasse-D-Gate).");
            await run.FinishSuccessAsync(new Dictionary<string, int> { ["uebersprungen_hierarchisch"] = 1 },
                messageSuffix: "[hierarchischer Modus: uebersprungen]", ct: ct);
            return new SyncResult(0, 0, 0);
        }
```
`FaWorkStepDetectionService.DetectAsync` direkt nach `await using var run = …` (Zeile 35) — identischer Block mit Log-Praefix `FaWorkStepDetection:`.
`BomCacheSyncService.SyncBomCacheAsync` direkt nach `await using var run = …` (Zeile 38) — identischer Block mit Praefix `BOM-Cache-Sync:`.
`BomCacheSyncService.SyncSpecificArticleNumbersAsync` als ERSTE Anweisung (vor dem Null-Check Zeile 175):
```csharp
        if (await _hierarchicalMode.IsHierarchicalAsync(ct))
        {
            _logger.LogInformation("BOM-Cache-Sync (spezifisch): uebersprungen — hierarchischer Modus (BOM-Bridge, Klasse-D-Gate).");
            return new SyncResult(0, 0, 0);
        }
```

- [ ] **Step 5: Bestehende Konstruktor-Aufrufe in Tests anpassen**

`grep -rn "new CoatingDetectionService(\|new FaWorkStepDetectionService(\|new BomCacheSyncService(" IDEALAKEWMSService.Tests` → an jeder Stelle `new StubHierarchicalModeReader(false)` als zweiten Parameter einfuegen (z. B. `FaWorkStepDetectionServiceTests.CreateService`: `new(ctx, new StubHierarchicalModeReader(false), NullLogger<…>.Instance, syncLogger ?? new FakeSyncLogger())`).

- [ ] **Step 6: Build + Service-Tests + Commit**

Run: `dotnet build IdealAkeWms.slnx`; `dotnet test IDEALAKEWMSService.Tests` → 232 + 4 gruen.
```bash
git add IDEALAKEWMSService IDEALAKEWMSService.Tests
git commit -m "wip: bom-bridge Task 4 — Klasse-D-Gates (Coating/WorkStep/BomCache x2) ueber IHierarchicalModeReader"
```

---

### Task 5: Views — Picking-Stueckliste, Artikelinfo, Navigation (Skill `frontend-design` VOR Beginn aufrufen)

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs:219-240`, `IdealAkeWms/Views/Picking/Bom.cshtml` (thead 163-190, Zeile 227-241 Pos.-Zelle, tbody-Zellen nach Zeile 256), `IdealAkeWms/Controllers/ArticlesController.cs:258-289`, `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs` + `ProductionOrderRepository.cs` (+`GetBySubOrderNumbersAsync`), `IdealAkeWms/Views/Articles/Info.cshtml:129-170`, `IdealAkeWms/Views/Shared/_Layout.cshtml:112-120, 177-189`
- Test: `IdealAkeWms.Tests/Controllers/ArticlesControllerTests.cs` (+1), `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs` oder passende bestehende Repo-Testdatei (+1)

**Interfaces:**
- Consumes: Task 3 (`BomViewModel.Hierarchical/FullStructure`, `BomItemViewModel`-Zusatzfelder); Task 2 Reverse-Lookup-Semantik (Eltern-SubOrderNumbers).
- Produces: `IProductionOrderRepository.GetBySubOrderNumbersAsync(IReadOnlyCollection<string> subOrderNumbers) : Task<List<ProductionOrder>>` (inkl. `PickingStatus`).

- [ ] **Step 1: `ColumnDefinitions.Bom` erweitern** — nach `new ColumnDef("category", …)` (Zeile 233) einfuegen:
```csharp
            // BOM-Bridge (hierarchisch, nur bei Model.Hierarchical gerendert; Registrierung ist Pflicht fuer Filter/Prefs)
            new ColumnDef("kommissionieren", "Komm.-Ziel",    Locked: false),
            new ColumnDef("hauptlagerplatz", "Hauptlagerplatz",Locked: false),
            new ColumnDef("sage-position",   "Sage-Pos.",      Locked: false, DefaultHidden: true),
            new ColumnDef("ebene",           "Ebene",          Locked: false, DefaultWidth: 60),
            new ColumnDef("vater-sub-fa",    "Vater-Sub-FA",   Locked: false),
```

- [ ] **Step 2: `Bom.cshtml`** — Kopfzeile nach `<th data-filterable data-col-key="category">Kategorie</th>` (Zeile 176):
```cshtml
                @if (Model.Hierarchical)
                {
                    <th data-filterable data-col-key="kommissionieren">Komm.-Ziel</th>
                    <th data-filterable data-col-key="hauptlagerplatz">Hauptlagerplatz</th>
                    <th data-filterable data-col-key="sage-position" title="Position laut Sage-Stueckliste">Sage-Pos.</th>
                    @if (Model.FullStructure)
                    {
                        <th class="text-end" data-filterable data-col-key="ebene">Ebene</th>
                        <th data-filterable data-col-key="vater-sub-fa">Vater-Sub-FA</th>
                    }
                }
```
Zeile 239 `@item.Position` ersetzen durch:
```cshtml
                        <span @(Model.Hierarchical && !string.IsNullOrEmpty(item.SagePosition) ? $"title=\"Sage-Position: {item.SagePosition}\"" : "")>@item.Position</span>
                        @if (item.IsWaise) { <span class="badge bg-warning text-dark ms-1" title="Waise: Elternknoten nicht in der Struktur">Waise</span> }
                        @if (item.PositionsKollision) { <span class="badge bg-danger ms-1" title="Zwei Positionen mit gleicher Nummer unter demselben FA — Pfad wurde eindeutig gemacht">Kollision</span> }
```
Zellen nach `<td>@(item.KategorieName ?? "")</td>` (Zeile 256):
```cshtml
                    @if (Model.Hierarchical)
                    {
                        <td>@item.Kommissionieren</td>
                        <td>@item.Hauptlagerplatz</td>
                        <td>@item.SagePosition</td>
                        @if (Model.FullStructure)
                        {
                            <td class="text-end">@item.Ebene</td>
                            <td>@item.VaterSubFa</td>
                        }
                    }
```
Spaltenzahl-Kommentar/Logik fuer leere Zeilen (Zeile ~339 „Spaltenzahl: 12 Basis …") um `Model.Hierarchical ? 3 : 0` + `Model.FullStructure ? 2 : 0` erweitern (Colspan der Leer-/Hinweiszeile). Die Print-`colNames`-Map (JS, Zeile ~984) bleibt unveraendert — neue Spalten liegen hinter Index 8.
Badges nutzen Bootstrap-Standard (`bg-warning text-dark`, `bg-danger` mit Text) — Farbe nie alleiniger Traeger, Text „Waise"/„Kollision" steht dabei (WCAG AA wie im Paket).

- [ ] **Step 3: `IProductionOrderRepository.GetBySubOrderNumbersAsync`**

Interface (neben `GetByArticleNumbersAsync`):
```csharp
    /// <summary>Auftraege zu Sub-FA-Nummern (BOM-Bridge Artikelinfo, hierarchisch), inkl. PickingStatus.</summary>
    Task<List<ProductionOrder>> GetBySubOrderNumbersAsync(IReadOnlyCollection<string> subOrderNumbers);
```
Implementierung in `ProductionOrderRepository` (Muster von `GetByArticleNumbersAsync` uebernehmen, inkl. `.Include(o => o.PickingStatus)`):
```csharp
    public async Task<List<ProductionOrder>> GetBySubOrderNumbersAsync(IReadOnlyCollection<string> subOrderNumbers)
    {
        if (subOrderNumbers.Count == 0) return new List<ProductionOrder>();
        var set = subOrderNumbers.ToList();
        return await _context.ProductionOrders
            .Include(o => o.PickingStatus)
            .Where(o => set.Contains(o.SubOrderNumber))
            .ToListAsync();
    }
```
Falls ein Cache-/Decorator-Repository `IProductionOrderRepository` implementiert (`grep -rn "IProductionOrderRepository" IdealAkeWms/Data/Repositories/*.cs`), dort die Methode 1:1 durchreichen. Repository-Test: InMemory, zwei Auftraege mit Sub-FAs `"1"`/`"2"`, Abfrage `["2"]` → genau der zweite.

- [ ] **Step 4: `ArticlesController.Info` hierarchischer Zweig** — Block Zeile 258-289 ersetzen:
```csharp
        var hierarchical = IsTrue(await _serviceSettingRepository.GetValueAsync(
            IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master));

        // Production orders containing this article (via BOM-Bridge / BOM cache)
        var deviceKeys = await _bomCacheRepository.GetDeviceArticleNumbersByComponentAsync(article.ArticleNumber);
        var usedInOrders = new List<ArticleUsageItem>();
        decimal plannedConsumption = 0m;
        if (deviceKeys.Count > 0)
        {
            // Hierarchisch: Keys sind SubOrderNumbers der Eltern-Sub-FAs (Partition DirectChildren);
            // flach: Geraete-Artikelnummern wie bisher.
            var orders = hierarchical
                ? await _productionOrderRepository.GetBySubOrderNumbersAsync(deviceKeys)
                : await _productionOrderRepository.GetByArticleNumbersAsync(deviceKeys);

            var openOrders = orders
                .Where(o => !o.IsDone && !o.IsCancelled && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking))
                .ToList();

            usedInOrders = openOrders
                .OrderBy(o => o.OrderNumber).ThenBy(o => o.SubOrderNumber)
                .Select(o => new ArticleUsageItem
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    ProductionDate = o.ProductionDate,
                    DeliveryDate = o.DeliveryDate,
                    SubOrderNumber = o.SubOrderNumber
                })
                .ToList();

            var mengePerDevice = await _bomCacheRepository.GetComponentMengePerDeviceAsync(article.ArticleNumber);
            // Hierarchisch: Sollmenge je Eltern-Sub-FA ist bereits die Auftragsgesamtmenge (keine × Quantity).
            plannedConsumption = hierarchical
                ? openOrders.Sum(o => mengePerDevice.GetValueOrDefault(o.SubOrderNumber))
                : openOrders.Where(o => o.ArticleNumber != null)
                    .Sum(o => mengePerDevice.GetValueOrDefault(o.ArticleNumber!) * o.Quantity);
        }
```
und beim ViewModel `Hierarchical = hierarchical,` (statt des erneuten Settings-Reads Zeile 304-305). Flachmodus: `OrderBy(o => o.ProductionDate)` beibehalten — dazu die Sortierung als `hierarchical ? … : orders.OrderBy(o => o.ProductionDate)` formulieren, damit AKE bit-identisch bleibt.

- [ ] **Step 5: `Views/Articles/Info.cshtml`** — Tabelle Zeile 136-154 hierarchisch umbauen:
```cshtml
                                <tr>
                                    <th>@(Model.Hierarchical ? "HauptFA" : "FA Nr.")</th>
                                    @if (Model.Hierarchical) { <th>Sub-FA (Baugruppe)</th> }
                                    <th>Fertigungstermin</th>
                                    <th>Liefertermin</th>
                                </tr>
```
Zeile:
```cshtml
                                        <td><a asp-controller="Picking" asp-action="Bom" asp-route-id="@order.Id"><strong>@order.OrderNumber</strong></a></td>
                                        @if (Model.Hierarchical) { <td>@order.SubOrderNumber</td> }
```
Footer (Zeile 162): `@Model.UsedInHauptFaCount Aufträge · @Model.UsedInOrders.Count Sub-FAs offen (basierend auf FA-Struktur, Mengen = Sollmenge)`. Dazu im Abschnitt „Geplanter Verbrauch" (dl weiter oben in der Datei, `grep -n "PlannedConsumption" Info.cshtml`) das Label hierarchisch um „(Sollmenge)" ergaenzen.

- [ ] **Step 6: `_Layout.cshtml`** — Zeile 112-120 (eigenstaendiges Kommissionierlisten-Item) entfernen; Zeile 177-189 ersetzen:
```cshtml
                        @* Kommissionierung — Dropdown: Picking-Workflow + IDEAL-Kommissionierlisten (Teil 3) *@
                        @if (canPick || (canProcessLager && faKommListenAktiv))
                        {
                            <li class="nav-item dropdown">
                                <a class="nav-link dropdown-toggle @((ViewContext.RouteData.Values["controller"]?.ToString() is "Picking" or "FaHierarchyKommissionierListen") ? "active" : "")" href="#" role="button" data-bs-toggle="dropdown" aria-expanded="false">
                                    Kommissionierung
                                    @if (canPick && leitstandAktiv && releasedPickingCount > 0)
                                    {
                                        <span class="badge rounded-pill" style="background-color: var(--ake-orange); font-size: 0.7em;">@releasedPickingCount</span>
                                    }
                                </a>
                                <ul class="dropdown-menu">
                                    @if (canPick)
                                    {
                                        <li><a class="dropdown-item" asp-controller="Picking" asp-action="Index">Kommissionierung</a></li>
                                    }
                                    @if (canProcessLager && faKommListenAktiv)
                                    {
                                        <li><a class="dropdown-item" asp-controller="FaHierarchyKommissionierListen" asp-action="Index">Kommissionierlisten</a></li>
                                    }
                                </ul>
                            </li>
                        }
```

- [ ] **Step 7: Tests**

`ArticlesControllerTests.cs` — neuer Test:
```csharp
    [Fact]
    public async Task Info_Hierarchical_ResolvesParentSubFas_AndSumsSollmengeWithoutQuantity()
    {
        var (ctrl, article, stock, _, _, bom, orders) = Build(hierarchical: true);
        article.Setup(a => a.GetByArticleNumberAsync("COMP")).ReturnsAsync(new Article { Id = 1, ArticleNumber = "COMP", Unit = "Stk" });
        stock.Setup(s => s.GetCurrentStockAsync("COMP", null, null, null)).ReturnsAsync(new List<StockOverviewItem>());
        bom.Setup(b => b.GetDeviceArticleNumbersByComponentAsync("COMP")).ReturnsAsync(new List<string> { "1043421", "1043422" });
        bom.Setup(b => b.GetComponentMengePerDeviceAsync("COMP")).ReturnsAsync(new Dictionary<string, decimal> { ["1043421"] = 5m, ["1043422"] = 2m });
        orders.Setup(o => o.GetBySubOrderNumbersAsync(It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync(new List<ProductionOrder>
        {
            new() { Id = 1, OrderNumber = "1035235", SubOrderNumber = "1043421", ArticleNumber = "A1", Quantity = 4m, PickingStatus = new ProductionOrderPickingStatus() },
            new() { Id = 2, OrderNumber = "1035235", SubOrderNumber = "1043422", ArticleNumber = "A2", Quantity = 4m, PickingStatus = new ProductionOrderPickingStatus() },
        });

        var vm = (await ctrl.Info("COMP")).Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<ArticleInfoViewModel>().Subject;

        vm.UsedInOrders.Should().HaveCount(2);
        vm.UsedInHauptFaCount.Should().Be(1);
        vm.PlannedConsumption.Should().Be(7m);          // 5 + 2, KEINE × Quantity
        orders.Verify(o => o.GetByArticleNumbersAsync(It.IsAny<List<string>>()), Times.Never);
    }
```
Bestehender Test `Info_Hierarchical_CountsHauptFaAndSubFa_ForZ4` (Zeile 113ff) mockt vermutlich `GetByArticleNumbersAsync` → auf `GetBySubOrderNumbersAsync` umstellen.

- [ ] **Step 8: Build + Tests + Commit**

`dotnet build IdealAkeWms.slnx`; `dotnet test IdealAkeWms.Tests` gruen.
```bash
git add -A IdealAkeWms IdealAkeWms.Tests
git commit -m "wip: bom-bridge Task 5 — Bom.cshtml hierarchische Spalten, Artikelinfo HauptFA/Sub-FA, Menue Kommissionierung"
```

---

### Task 6: Version, Changelog, TESTSZENARIEN TS-70, Hilfe, feature-complete, QA, Brain

**Files (Worktree):** `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/AppVersion.cs`, `IdealAkeWms/Views/Help/Changelog.cshtml`, `IdealAkeWms/Views/Help/Index.cshtml`, `docs/TESTSZENARIEN.md`
**Files (Hauptcheckout `secondbrain/`):** Spec (Status/QA/Deploy), `aufgaben/2026-09-08-bom-schnittstellen-bridge-hierarchisch-umsetzung.md`, `architektur/adr/0013-bom-bridge-repository-schnittstelle-statt-cache-kopie.md` (neu), `architektur/fallstricke.md` (§10 neu), `codebase/services.md`, `feature-map.md`, `changelog/2026-09-08-v1-36-0-ideal-bom-bridge.md` (neu), `tests/testszenarien-index.md`

- [ ] **Step 1: Version + Anwender-Changelog**

Beide `AppVersion.cs`: `Version = "1.36.0"`, `Date = "2026-09-08"`. In `Changelog.cshtml` VOR der v1.35.0-Karte (Zeile 10) eine neue Karte gleicher Struktur (Header `<strong>v1.36.0</strong> <span class="text-white-50 ms-2">08.09.2026</span>`) mit `<h6>IDEAL-Standort: Stückliste im hierarchischen Modus (BOM-Bridge)</h6>` und Punkten: Stückliste am HauptFA = komplette Struktur, am Sub-FA = eigene Stückliste; Spalten Komm.-Ziel/Hauptlagerplatz/Sage-Pos. (Filter); Mengen = Sollmenge; Artikelinfo zeigt HauptFA + Sub-FA; Menü „Kommissionierung" bündelt Workflow + Kommissionierlisten; AKE unverändert.

- [ ] **Step 2: Hilfe** — `Views/Help/Index.cshtml` Zeile 120-121 (Artikelinfo-Absatz) um einen Satz ergaenzen: „Am Standort IDEAL (hierarchische Aufträge) steht hier die Haupt-FA als Gerät und die Baugruppe (Sub-FA), in der das Teil verbaut ist; Mengen sind Sollmengen." Im Kommissionierungs-Abschnitt (`grep -n "Kommissionierung" Index.cshtml`, Stueckliste/BOM-Absatz) einen Absatz: „Hierarchischer Modus: Am HauptFA zeigt die Stückliste die komplette Struktur (Spalte Ebene/Vater-Sub-FA), am Sub-FA nur dessen eigene Positionen. Spalte Komm.-Ziel filtert nach Kommissionier-Ziel; Sage-Pos. zeigt die Original-Position, die Pos.-Spalte den Pfad."

- [ ] **Step 3: `docs/TESTSZENARIEN.md`** — am Ende (nach Zeile 7198) neues Kapitel `## TS-70 — IDEAL: BOM-Bridge — Stueckliste ueber die Schnittstelle (v1.36.0)` mit Vorbemerkung (Master true, materialisierter HauptFA ≥ 2 Ebenen; automatisiert: `FaHierarchyBomRepositoryTests` (10), `BomRepositoryMasterSwitchTests` (4), `BomQuantityResolverTests` (2), `HierarchicalModeGateTests` (4), Controller-/Builder-/Artikelinfo-Tests) und den acht Szenarien **TS-70.1 … TS-70.8 wortgleich aus dem Spec-Abschnitt „Test-Szenarien / Manual-UAT"** (HauptFA-Vollansicht inkl. Negativfall; Sub-FA-Ansicht; Kommissionieren-Filter inkl. Negativfall; Artikelinfo; Menue; Flachmodus bit-identisch; Klasse-D-Gate im Protokoll; Guard ersetzt) plus **TS-70.9 Sage-Position sichtbar** (Tooltip + Spalte `Sage-Pos.` per Zahnrad; Negativ: flach keine solche Spalte) und **TS-70.10 Positions-Kollision** (zwei Geschwister gleiche Position → Badge „Kollision", Pfad `n~2`, Warnung im Log; keine Zeile fehlt).

- [ ] **Step 4: feature-complete Commit**

```bash
git add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml IdealAkeWms/Views/Help/Index.cshtml docs/TESTSZENARIEN.md
git commit -m "wip: bom-bridge feature-complete — v1.36.0, Changelog, TS-70, Hilfe"
```

- [ ] **Step 5: Verifikation + Code-Review + qa-agent** (superpowers:verification-before-completion; qa-agent prueft Build/Tests/TS-70, setzt Status, schreibt QA-Nachweis + Deploy-Abschnitt in die Spec im Hauptcheckout; Buendel-Aufgaben erhalten Stand-Eintrag).

- [ ] **Step 6: Brain (Hauptcheckout)** — ADR 0013 (Format wie 0012: Kontext/Optionen „Cache speisen" vs. „Bridge"/Entscheidung/Konsequenzen, mit den vier Befunden aus der Spec), fallstricke §10 „IDEAL — BOM-Bridge" (BomItem/SqlQueryRaw-Property-Zwang; Baugruppe = Eltern-Artnr-String, IsBaugruppe abgeleitet; rekursiver Pfad statt Praefix; DI-Zyklus → Lazy-Delegates; Klasse-D-Gates DB-first ueber Reader), `codebase/services.md` (Repository-Tabelle: `FaHierarchyBomRepository`, `BomRepositoryMasterSwitch`; Guard-Zeile ersetzen; Service-Abschnitt `IHierarchicalModeReader`), `feature-map.md` (BOM-Guard-Zeile → „abgeloest durch BOM-Bridge v1.36.0", neue Zeile), `changelog/2026-09-08-v1-36-0-ideal-bom-bridge.md`, `tests/testszenarien-index.md` (Zeile 94 TS-68 „abgeloest durch TS-70", neue Zeile 70). Commit `brain: bom-schnittstellen-bridge-hierarchisch testbereit`.

---

## Self-Review (durchgefuehrt beim Schreiben)

- **Spec-Abdeckung:** In-Scope 1 (Task 2+3), 2 (Task 3), 3 (Task 1), 4 (Task 1), 5 (Task 2 Mapping), 6 (Task 1 `BomScopes.ForOrder` + Task 2), 7 (Task 5), 8 (Task 5), 9 (Task 4), 10 (Task 5), 11 (Spec Design I, verifiziert — kein Code). Freigabe-Bedingung (a) → `SagePosition` + Tooltip/Spalte (Task 2/5); (b) → `Unique()` + `PositionsKollision` + Log (Task 2) + Badge (Task 5). AK 1-16 abgedeckt; AK 15 (`HasGlass`) ist laut Spec kein Aenderungsbedarf (bestehender Toggle-Test bleibt).
- **Abweichung vom Spec-Wortlaut (bewusst, in Aufgabe zu dokumentieren):** `BomKey` traegt zusaetzlich `OrderNumber` (HauptFA) fuer den gecachten Gruppen-Lookup; Master-Read im Service ueber `IHierarchicalModeReader` statt direkt `ServiceSettings.GetBoolSafeAsync` (Testbarkeit, DB-first bleibt); `FullStructure` ist an einem Sub-FA = DirectChildren (Fallback statt Subtree — der Resolver fragt dort ohnehin DirectChildren an).
- **Typ-Konsistenz:** `BomKey(ArticleNumber, SubOrderNumber, OrderNumber)` ueberall; `GetBomItemsAsync(BomKey, BomScope)`; `GetByArticleNumberAsync(BomKey)`; `MengeIstAuftragsmenge`; `FaHierarchyBomItem`-Felder in Task 2/3/5 identisch; `GetBySubOrderNumbersAsync(IReadOnlyCollection<string>)` in Task 5 Interface/Impl/Test/Controller identisch.
- **Platzhalter:** Die drei Interface-Weiterleitungen (`GetArticleNumbersWithCoatingPartsAsync`/`GetHeaderHashesAsync`/`UpsertBomAsync`) verweisen auf die exakten Parameterlisten in `IBomCacheRepository.cs:19-42` — dort ablesen, nicht raten.
