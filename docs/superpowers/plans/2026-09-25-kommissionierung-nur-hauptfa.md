# Kommissionierung nur am HauptFA — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Freigabe und interaktive Kommissionierung strukturell auf die HauptFA beschränken (eine Sub-FA-Definition), die Freigabe-Kaskade zurückbauen, „Alle Ziele“ (`!(leer)`) im Komm.-Ziel-Filter der Stückliste anbieten und die Kommissionierlisten-Relevanzregel extrahieren.

**Architecture:** Eine einzige Regel `ProductionOrder.IsHauptFa` (EF-übersetzbare Expression). Alles andere wird aus ihr abgeleitet: `IsSubFa` (kompiliert) für geladene Entitäten, `IsSubFaOf(...)` für Projektionen (`LeitstandOrderRow`) und `.Where(IsHauptFa)` für die vier Warteschlangen-Abfragen. Die Stückliste bekommt keine neue Relevanzregel, sondern einen Bedeutungswert `!(leer)` im bestehenden Komm.-Ziel-Filter. Freitext-Profilwerte werden beim Speichern normalisiert.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10 (SQL Server, InMemory in Tests), xUnit + FluentAssertions + Moq, Razor + jQuery/Select2.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-09-25-kommissionierung-nur-hauptfa-spec.md` (Hauptcheckout; Rumpf + `affected_code` = Auftrag; bei Widerspruch gilt der **FREIGABE-NACHTRAG** am Dateiende).

Arbeitsverzeichnis für ALLE Befehle: `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`. **Niemals** in `secondbrain/` schreiben (existiert im Worktree nicht; Brain-Updates macht der Orchestrator im Hauptcheckout). Nie `git add -A`, nur explizite Pfade stagen. `CLAUDE.md` im Worktree ist lokal geändert — nicht anfassen, nicht stagen.

## Global Constraints

- Version: `1.45.0` → `1.46.0` in `IdealAkeWms/AppVersion.cs` UND `IDEALAKEWMSService/AppVersion.cs`.
- Testszenarien-Kapitel: **TS-79** (letztes vorhandenes: TS-78).
- Keine Migration, kein `SQL/XX`-Skript, keine Service-Änderung.
- **EINE Sub-FA-Definition:** `ProductionOrder.IsHauptFa`. Nirgends (Abfragen, Controller, Views, Tests-Helfer) eine weitere Vergleichsformel `SubOrderNumber == OrderNumber` o. ä. schreiben.
- **HARTE BEDINGUNG:** Bestehende `SetReleaseBatchAsync`-/`BulkRelease`-Tests laufen OHNE Änderung ihrer Fixtures grün. Muss ein Fixture geändert werden → STOP, als Befund melden, nicht reparieren.
- TempData kennt nur `SuccessMessage` und `WarningMessage`.
- Code/Bezeichner Englisch, UI-Texte Deutsch. Exakte UI-Texte:
  - `Kommissionierung erfolgt nur am HauptFA {OrderNumber}.`
  - `Alle Ziele (nur kommissionier-relevant)`
  - `Alle Ziele schließt Einzelziele aus — gespeichert wurde nur 'Alle Ziele'.`
  - `Keine sichtbaren Positionen — nichts zu drucken.`
- `wwwroot/js/table-filter.js` NICHT anfassen.
- `CascadeDonePreview`/`CascadeDone` NICHT anfassen.
- Build/Test: `dotnet build IdealAkeWms.slnx` und `dotnet test IdealAkeWms.Tests` (nie parallel zu einem anderen Build im selben Worktree).

## Review Focus

1. **AKE-Zeile mit leerem/Null-`SubOrderNumber` in einer Projektion** — muss als HauptFA gelten (Controls sichtbar, keine Sperre). Test in Task 2 (`IsSubFaOf(order, null)` → false).
2. **Bulk-Zurücknehmen einer bereits freigegebenen Sub-FA** — wird übersprungen und gemeldet, nicht still zurückgesetzt. Test in Task 2.
3. **Profilwert `!(LEER)` (nur Schreibweise)** — normalisiert, aber KEIN Hinweis; `!(leer),KA-02` — normalisiert MIT Hinweis. Tests in Task 5.
4. **Stückliste ohne ein einziges Komm.-Ziel + „Alle Ziele“ + Drucken** — kein Druckfenster, Leerzustand sichtbar (manuell, TS-79; Task 6).
5. **Warteschlange mit alter, freigegebener Sub-FA** — erscheint nicht in `/Picking`, zählt nicht in der Kennzahl, beeinflusst Max-Priorität nicht. Test in Task 2.

---

### Task 1: Rückbau der Freigabe-Kaskade

**Files:**
- Modify: `IdealAkeWms/Controllers/PickingLeitstandController.cs` (Actions `CascadeReleasePreview`/`CascadeRelease` inkl. Kommentarblock, ca. Z. 543-616, entfernen)
- Modify: `IdealAkeWms/Data/Repositories/IProductionOrderPickingStatusRepository.cs` (`SetReleaseForOrderNumberAsync`, `CountReleasedByOrderNumberAsync` entfernen)
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs` (deren Implementierungen entfernen, ca. Z. 283-335)
- Modify: `IdealAkeWms/Views/PickingLeitstand/Index.cshtml` (Button `.btn-cascade-release` Z. ~206-217, Modal `cascadeReleaseModal` Z. ~398-451, zugehöriges `<script>` ab Z. ~556 entfernen)
- Modify: `IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs`, `IdealAkeWms.Tests/Repositories/ProductionOrderPickingStatusRepositoryTests.cs` (nur die Tests zu diesen Methoden/Actions entfernen)

**Interfaces:** Produces: nichts Neues. Consumes: nichts.

- [ ] **Step 1:** Alle Fundstellen listen: `grep -rEn "CascadeRelease|SetReleaseForOrderNumber|CountReleasedByOrderNumber|btn-cascade-release|cascadeRelease" IdealAkeWms IdealAkeWms.Tests`
- [ ] **Step 2:** Entfernen wie oben. `CascadeDone*`, `btn-cascade-done`, `cascadeDoneModal` und deren JS bleiben unverändert. In `Index.cshtml` darauf achten, dass das `<script>`-Tag der Fertigmeldungs-Kaskade intakt bleibt (nur den Release-Block entfernen).
- [ ] **Step 3:** Nachweis: `grep -rEn "CascadeRelease|SetReleaseForOrderNumber|CountReleasedByOrderNumber" IdealAkeWms IdealAkeWms.Tests` → **0 Treffer** (Ausgabe festhalten).
- [ ] **Step 4:** `dotnet build IdealAkeWms.slnx` → 0 Fehler; `dotnet test IdealAkeWms.Tests` → grün.
- [ ] **Step 5:** Commit: `git add <die geänderten Dateien einzeln> && git commit -m "wip: 2026-09-25-kommissionierung-nur-hauptfa rueckbau freigabe-kaskade"`

---

### Task 2: Eine Sub-FA-Definition + Warteschlange + Bulk-Skip

**Files:**
- Modify: `IdealAkeWms/Models/ProductionOrder.cs`
- Modify: `IdealAkeWms/Data/Repositories/IProductionOrderPickingStatusRepository.cs` (`BulkReleaseResult.SkippedSubFa`)
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs` (4 Abfragen Z. ~173-216, `SetReleaseBatchAsync` Z. ~218-281)
- Create: `IdealAkeWms.Tests/Models/ProductionOrderHauptFaTests.cs`
- Modify: `IdealAkeWms.Tests/Repositories/ProductionOrderPickingStatusRepositoryTests.cs` (NEUE Tests anhängen, bestehende Fixtures NICHT ändern)

**Interfaces — Produces:**
- `public static readonly Expression<Func<ProductionOrder, bool>> ProductionOrder.IsHauptFa`
- `public bool ProductionOrder.IsSubFa` (`[NotMapped]`, get-only)
- `public static bool ProductionOrder.IsSubFaOf(string orderNumber, string? subOrderNumber)` (für Projektionen; `null` → wie `""` → HauptFA)
- `public List<string> BulkReleaseResult.SkippedSubFa`

- [ ] **Step 1: Failing tests** — `IdealAkeWms.Tests/Models/ProductionOrderHauptFaTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Models;
using Xunit;

namespace IdealAkeWms.Tests.Models;

public class ProductionOrderHauptFaTests
{
    [Theory]
    [InlineData("", false)]          // leer gilt als HauptFA (AKE-sichere Richtung)
    [InlineData("100", false)]       // Sub == Order -> HauptFA
    [InlineData("100-7", true)]      // Sub != Order -> Sub-FA
    public void IsSubFa_FollowsIsHauptFa(string sub, bool expected)
    {
        new ProductionOrder { OrderNumber = "100", SubOrderNumber = sub }.IsSubFa.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]        // Projektions-Default: wie "" -> HauptFA
    [InlineData("", false)]
    [InlineData("100", false)]
    [InlineData("100-7", true)]
    public void IsSubFaOf_MatchesEntityRule(string? sub, bool expected)
    {
        ProductionOrder.IsSubFaOf("100", sub).Should().Be(expected);
    }
}
```

Repository-Test (in `ProductionOrderPickingStatusRepositoryTests.cs` anhängen; vorhandene Hilfsmethoden/Kontext-Erzeugung der Datei wiederverwenden — erst lesen, wie dort `ApplicationDbContext` InMemory erzeugt und Orders/Status angelegt werden). Vier freigegebene Orders mit `OrderNumber = "100"` und `SubOrderNumber` = `""` (Id 1), `"100"` (Id 2), `"100-7"` (Id 3), sowie eine vierte HauptFA `OrderNumber="200", SubOrderNumber="200"` (Id 4), alle mit `ArticleNumber` gesetzt, `PickingStatus` mit `IsReleasedForPicking = true`, `PickingPriority` = Id*10, `AssignedPickerId = 5`:

```csharp
[Fact]
public async Task Queue_ContainsOnlyHauptFa_EvenIfSubFaIsReleased()
{
    // arrange: s. o. (Sub-FA Id 3 hat Priority 30 = hoechste)
    var repo = new ProductionOrderPickingStatusRepository(context);

    (await repo.GetReleasedForPickingAsync()).Select(o => o.Id).Should().BeEquivalentTo(new[] { 1, 2, 4 });
    (await repo.GetReleasedForPickingByPickerAsync(5)).Select(o => o.Id).Should().BeEquivalentTo(new[] { 1, 2, 4 });
    (await repo.GetReleasedForPickingCountAsync()).Should().Be(3);
    (await repo.GetMaxPickingPriorityAsync()).Should().Be(40);
    (await repo.GetMaxPickingPriorityAsync(excludeProductionOrderId: 4)).Should().Be(20); // 30 (Sub-FA) zaehlt nicht
}

[Theory]
[InlineData(true)]
[InlineData(false)]
public async Task SetReleaseBatch_SkipsSubFa_InBothDirections(bool release)
{
    // arrange: Id 2 (HauptFA "100"/"100") und Id 3 (Sub-FA "100"/"100-7"), Status IsReleasedForPicking = !release
    var result = await repo.SetReleaseBatchAsync(new[] { 2, 3 }, release, null, null, "t", "t", "t");

    result.Processed.Should().Be(1);
    result.SkippedSubFa.Should().ContainSingle();
    (await context.ProductionOrderPickingStatuses.SingleAsync(s => s.ProductionOrderId == 3))
        .IsReleasedForPicking.Should().Be(!release); // unveraendert
}
```

(`SkippedSubFa` enthält die `SubOrderNumber` der übersprungenen Zeile — damit ist die Meldung eindeutig; bei HauptFA wäre `OrderNumber` mehrdeutig. Assertion: `result.SkippedSubFa.Should().Equal("100-7")`.)

- [ ] **Step 2:** `dotnet test IdealAkeWms.Tests --filter "FullyQualifiedName~ProductionOrderHauptFaTests|FullyQualifiedName~Queue_ContainsOnlyHauptFa|FullyQualifiedName~SetReleaseBatch_SkipsSubFa"` → FAIL (Kompilierfehler).

- [ ] **Step 3: Implementierung `ProductionOrder.cs`** (Usings `System.ComponentModel.DataAnnotations.Schema`, `System.Linq.Expressions` ergänzen; Block direkt unter `SubOrderNumber`):

```csharp
    // EINE Sub-FA-Definition (Spec 2026-09-25-kommissionierung-nur-hauptfa). Quelle ist die
    // Expression (EF uebersetzt sie in SQL); IsSubFa/IsSubFaOf sind daraus kompiliert — keine
    // zweite Formel. Leer gilt als HauptFA (AKE-sichere Richtung). Grenze: InMemory-Tests belegen
    // nur C#-Semantik; SQL Server vergleicht case-insensitiv und ignoriert Trailing Spaces —
    // folgenlos, weil beide Spalten aus demselben Ursprungswert stammen.
    public static readonly Expression<Func<ProductionOrder, bool>> IsHauptFa =
        p => p.SubOrderNumber == "" || p.SubOrderNumber == p.OrderNumber;

    private static readonly Func<ProductionOrder, bool> IsHauptFaCompiled = IsHauptFa.Compile();

    [NotMapped]
    public bool IsSubFa => !IsHauptFaCompiled(this);

    /// <summary>Fuer Projektionen ohne ProductionOrder-Objekt (z. B. LeitstandOrderRow).
    /// null gilt wie "" als HauptFA.</summary>
    public static bool IsSubFaOf(string orderNumber, string? subOrderNumber) =>
        !IsHauptFaCompiled(new ProductionOrder { OrderNumber = orderNumber, SubOrderNumber = subOrderNumber ?? string.Empty });
```

- [ ] **Step 4: Repository** — `BulkReleaseResult` um `public List<string> SkippedSubFa { get; set; } = new();` erweitern. In `ProductionOrderPickingStatusRepository.cs`:
  - `GetReleasedForPickingAsync` und `GetReleasedForPickingByPickerAsync`: `_context.ProductionOrders.Where(ProductionOrder.IsHauptFa).Where(<bestehendes Prädikat>)…` (Rest unverändert).
  - `GetReleasedForPickingCountAsync` auf Wurzel `ProductionOrders`, Prädikat exakt übernommen:

```csharp
    public Task<int> GetReleasedForPickingCountAsync()
        => _context.ProductionOrders
            .Where(ProductionOrder.IsHauptFa)
            .CountAsync(p => p.PickingStatus != null && p.PickingStatus.IsReleasedForPicking
                             && !p.IsDone && !p.IsCancelled && !p.PickingStatus.IsDonePicking);
```

  - `GetMaxPickingPriorityAsync` auf Wurzel `ProductionOrders` (1:1 über UNIQUE `ProductionOrderId`):

```csharp
    public async Task<int> GetMaxPickingPriorityAsync(int? excludeProductionOrderId = null)
    {
        var q = _context.ProductionOrders
            .Where(ProductionOrder.IsHauptFa)
            .Where(p => p.PickingStatus != null && p.PickingStatus.IsReleasedForPicking
                        && !p.IsDone && !p.IsCancelled && p.PickingStatus.PickingPriority != null);

        if (excludeProductionOrderId.HasValue)
            q = q.Where(p => p.Id != excludeProductionOrderId.Value);

        return await q.MaxAsync(p => (int?)p.PickingStatus!.PickingPriority) ?? 0;
    }
```

  - `SetReleaseBatchAsync`: direkt nach `if (row.ProductionOrder == null) continue;`, VOR dem `SkippedNoArticle`-Zweig, ungated von `release`:

```csharp
            // Kommissionierung nur am HauptFA: Sub-FA in beiden Richtungen ueberspringen und melden.
            if (row.ProductionOrder.IsSubFa)
            {
                result.SkippedSubFa.Add(row.ProductionOrder.SubOrderNumber);
                continue;
            }
```

- [ ] **Step 5:** Tests aus Step 2 → PASS. Dann `dotnet test IdealAkeWms.Tests` komplett → grün. **Prüfen:** kein bestehendes Fixture geändert (`git diff IdealAkeWms.Tests` zeigt nur angehängte Tests).
- [ ] **Step 6:** Commit (explizite Pfade): `wip: 2026-09-25-kommissionierung-nur-hauptfa IsHauptFa + warteschlange + bulk-skip`

---

### Task 3: Server-Guards + Views auf HauptFA beschränkt

**Files:**
- Modify: `IdealAkeWms/Controllers/PickingController.cs` (`Bom`, Z. ~280-290)
- Modify: `IdealAkeWms/Controllers/PickingLeitstandController.cs` (`ToggleRelease` Z. ~356, `BulkRelease` Z. ~446, `MapItem` Z. ~118-160)
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs` (`MapItem` Z. ~160-190)
- Modify: `IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs` (`PickingLeitstandItem.IsSubFa`), `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs` (`ProductionOrderListItem.IsSubFa`)
- Modify: `IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml`, `IdealAkeWms/Views/ProductionOrders/_ProductionOrderRow.cshtml`
- Test: `IdealAkeWms.Tests/Controllers/PickingControllerTests.cs` (bzw. vorhandene Bom-Testdatei), `IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs`

**Interfaces — Consumes:** `ProductionOrder.IsSubFa`, `ProductionOrder.IsSubFaOf`, `BulkReleaseResult.SkippedSubFa` (Task 2). **Produces:** `bool PickingLeitstandItem.IsSubFa`, `bool ProductionOrderListItem.IsSubFa`.

**Pflicht vor den View-Änderungen:** Skill `frontend-design` aufrufen (CLAUDE.md), Leitplanken: Bootstrap 5, bestehende Muster, nichts Neues gestalten — hier wird nur bedingt ausgeblendet.

- [ ] **Step 1: Failing tests** (vorhandene Test-Setups der jeweiligen Controller-Testklassen wiederverwenden; erst lesen):
  - `Bom_SubFa_RedirectsToIndexWithWarning`: `_productionOrderRepo.GetByIdAsync(7)` liefert `new ProductionOrder { Id = 7, OrderNumber = "100", SubOrderNumber = "100-7", ArticleNumber = "A" }` → Ergebnis `RedirectToActionResult` mit `ActionName == "Index"`, `TempData["WarningMessage"] == "Kommissionierung erfolgt nur am HauptFA 100."`, und `_bomRepository` wurde nie aufgerufen.
  - `ToggleRelease_SubFa_NoChange_InBothDirections` (Theory `IsReleasedForPicking` true/false): Order Sub-FA wie oben, `GetByProductionOrderIdAsync` liefert Status → `SetReleaseAsync` wird `Times.Never` aufgerufen, `TempData["WarningMessage"] == "Kommissionierung erfolgt nur am HauptFA 100."`.
  - `BulkRelease_ReportsSkippedSubFa`: `SetReleaseBatchAsync` liefert `new BulkReleaseResult { Processed = 1, SkippedSubFa = { "100-7" } }` → `TempData["WarningMessage"]` enthält `"100-7"` und den Text `Übersprungen (Sub-FA — Kommissionierung nur am HauptFA)`.
- [ ] **Step 2:** Tests laufen lassen → FAIL.
- [ ] **Step 3: `PickingController.Bom`** direkt nach `if (order == null) return NotFound();`:

```csharp
        // Kommissionierung nur am HauptFA (Spec 2026-09-25): Sub-FA-Stueckliste nicht interaktiv.
        if (order.IsSubFa)
        {
            TempData["WarningMessage"] = $"Kommissionierung erfolgt nur am HauptFA {order.OrderNumber}.";
            return RedirectToAction(nameof(Index));
        }
```

- [ ] **Step 4: `ToggleRelease`** direkt nach `if (order == null) return NotFound();` (vor dem Laden von `ps`), richtungsunabhängig:

```csharp
        if (order.IsSubFa)
        {
            TempData["WarningMessage"] = $"Kommissionierung erfolgt nur am HauptFA {order.OrderNumber}.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }
```

- [ ] **Step 5: `BulkRelease`** — die bestehende `SkippedNoArticle`-Zuweisung überschreibt TempData; beide Hinweise zusammenführen:

```csharp
        var warnings = new List<string>();
        if (batch.SkippedNoArticle.Count > 0)
            warnings.Add($"Übersprungen (keine Artikelnummer): {string.Join(", ", batch.SkippedNoArticle)}");
        if (batch.SkippedSubFa.Count > 0)
            warnings.Add($"Übersprungen (Sub-FA — Kommissionierung nur am HauptFA): {string.Join(", ", batch.SkippedSubFa)}");
        if (warnings.Count > 0)
            TempData["WarningMessage"] = string.Join(" · ", warnings);
```

  (Bestehender Test auf den exakten `SkippedNoArticle`-Text muss unverändert grün bleiben — Text für diesen Fall bleibt identisch.)
- [ ] **Step 6: ViewModels + MapItem** — in beiden Items `public bool IsSubFa { get; set; }` (XML-Kommentar: „aus ProductionOrder.IsSubFaOf — keine eigene Formel“). In beiden `MapItem`: `IsSubFa = ProductionOrder.IsSubFaOf(o.OrderNumber, o.SubOrderNumber),`.
- [ ] **Step 7: Views** — `_ProductionOrderRow.cshtml` Z. 16-18 ersetzen durch `var isHauptFaRow = Model.Hierarchical && !item.IsSubFa;` (Kommentar anpassen: Quelle `ProductionOrder.IsSubFaOf`). `_PickingLeitstandRow.cshtml`: dieselbe Variable einführen. In BEIDEN Rows: Bulk-Checkbox-Zelle, Freigabe-Zelle (Toggle-Formular, Freigeben-Button, Prioritäts-Input) und den interaktiven Link `asp-controller="Picking" asp-action="Bom"` nur rendern, wenn `!Model.Hierarchical || isHauptFaRow`. **Wichtig:** Die `<td>` selbst bleibt immer stehen (sonst verschieben sich Spalten) — nur ihr Inhalt wird bedingt. Der `FaWorklist/Bom`-Link bleibt unverändert. Vorher lesen, wie die Zellen gegatet sind (`ColumnDefinitions.cs` Z. ~67-69: `bulk-select`/`release` nur bei `LeitstandAktiv && CanManagePickingRelease`).
- [ ] **Step 8:** Tests → PASS, `dotnet build IdealAkeWms.slnx` + `dotnet test IdealAkeWms.Tests` grün. `grep -rn "SubOrderNumber == item.OrderNumber\|SubOrderNumber != \|SubOrderNumber == o" IdealAkeWms/Views IdealAkeWms/Controllers` → keine neue Formel (bestehende fremde Stellen, z. B. Lookups, nicht anfassen; nur prüfen, dass Task 3 keine hinzugefügt hat).
- [ ] **Step 9:** Commit: `wip: 2026-09-25-kommissionierung-nur-hauptfa guards + views hauptfa-only`

---

### Task 4: Relevanzregel extrahieren

**Files:**
- Create: `IdealAkeWms/Services/KommissionierRelevanzFilter.cs`
- Modify: `IdealAkeWms/Services/KommissionierListenService.cs` (`BuildFlagPredicate` Z. ~228-236)
- Test: `IdealAkeWms.Tests/Services/KommissionierRelevanzFilterTests.cs`

- [ ] **Step 1: Failing test:**

```csharp
using FluentAssertions;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class KommissionierRelevanzFilterTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("KA-02", true)]
    public void IsRelevant(string? value, bool expected) =>
        KommissionierRelevanzFilter.IsRelevant(value).Should().Be(expected);
}
```

- [ ] **Step 2:** FAIL. **Step 3:** Implementierung:

```csharp
namespace IdealAkeWms.Services;

/// <summary>
/// Kommissionier-Relevanz einer Stuecklisten-Position (Spec 2026-09-25-kommissionierung-nur-hauptfa):
/// heute genau ein Kriterium — Feld <c>Kommissionieren</c> nicht leer. Einziger Verbraucher:
/// <see cref="KommissionierListenService.BuildFlagPredicate"/>. Ort fuer kuenftige Zusatzkriterien.
/// Die Stueckliste prueft dieselbe Semantik client-seitig ueber den Filterwert <c>!(leer)</c>
/// (Views/Picking/Bom.cshtml, bomMatchesFilter) — Aenderungen hier dort mitziehen.
/// </summary>
public static class KommissionierRelevanzFilter
{
    public static bool IsRelevant(string? kommissionieren) => !string.IsNullOrWhiteSpace(kommissionieren);
}
```

  In `BuildFlagPredicate` beide `!string.IsNullOrWhiteSpace(n.Kommissionieren)` durch `KommissionierRelevanzFilter.IsRelevant(n.Kommissionieren)` ersetzen (Rest unverändert).
- [ ] **Step 4:** Neuer Test + bestehende `KommissionierListenServiceTests` grün (AK 1: bestehende Tests unverändert).
- [ ] **Step 5:** Commit: `wip: 2026-09-25-kommissionierung-nur-hauptfa relevanzfilter`

---

### Task 5: Normalisierung des Komm.-Ziel-Standardfilters beim Speichern

**Files:**
- Create: `IdealAkeWms/Services/KommissionierzielFilterWert.cs`
- Modify: `IdealAkeWms/Controllers/AccountController.cs` (Z. ~219), `IdealAkeWms/Controllers/UsersController.cs` (Create Z. ~129, Edit Z. ~301)
- Modify: `IdealAkeWms/Views/Account/Profile.cshtml` (Z. ~53), `IdealAkeWms/Views/Users/Edit.cshtml` (Z. ~117) — Placeholder; ggf. `Views/Users/Create.cshtml` falls dort dasselbe Feld existiert
- Test: `IdealAkeWms.Tests/Services/KommissionierzielFilterWertTests.cs` + je ein Controller-Test in den vorhandenen Account-/Users-Controller-Testklassen

**Interfaces — Produces:** `public static (string? Value, bool DroppedTargets) KommissionierzielFilterWert.Normalize(string? input)`; `public const string AlleZiele = "!(leer)"`.

- [ ] **Step 1: Failing tests:**

```csharp
using FluentAssertions;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class KommissionierzielFilterWertTests
{
    [Theory]
    [InlineData("!(leer),KA-02", "!(leer)", true)]   // Einzelziel verworfen -> Hinweis
    [InlineData("KA-02, !(LEER)", "!(leer)", true)]
    [InlineData("!(LEER)", "!(leer)", false)]         // nur Schreibweise -> KEIN Hinweis
    [InlineData(" !(leer) ", "!(leer)", false)]
    [InlineData("!(leer)", "!(leer)", false)]
    [InlineData(" KA-02 ", "KA-02", false)]
    [InlineData("KA-02,KA-03", "KA-02,KA-03", false)]
    [InlineData("   ", null, false)]
    [InlineData(null, null, false)]
    public void Normalize(string? input, string? expected, bool dropped)
    {
        var (value, droppedTargets) = KommissionierzielFilterWert.Normalize(input);
        value.Should().Be(expected);
        droppedTargets.Should().Be(dropped);
    }
}
```

  Controller-Tests (vorhandenes Setup der Testklassen lesen und nutzen): Profil-POST und Users-Edit-POST mit `DefaultFilterBomKommissionierziel = "!(leer),KA-02"` → gespeicherter User hat `"!(leer)"`, `TempData["WarningMessage"] == "Alle Ziele schließt Einzelziele aus — gespeichert wurde nur 'Alle Ziele'."`, `SuccessMessage` weiterhin gesetzt. Mit `"!(LEER)"` → `"!(leer)"` gespeichert, `TempData["WarningMessage"]` ist null.
- [ ] **Step 2:** FAIL.
- [ ] **Step 3: Implementierung:**

```csharp
namespace IdealAkeWms.Services;

/// <summary>
/// Normalisiert den gespeicherten Standardfilter „Komm.-Ziel“ der Stueckliste
/// (User.DefaultFilterBomKommissionierziel, Freitext). <c>!(leer)</c> = „Alle Ziele“ (Ziel nicht leer)
/// ist exklusiv: kommt es — in beliebiger Schreibweise — vor, wird genau <see cref="AlleZiele"/>
/// gespeichert. <c>DroppedTargets</c> ist nur true, wenn dabei Einzelziele verworfen wurden
/// (Bedeutungsaenderung -> Hinweis); reine Schreibweisen-Korrektur meldet nichts.
/// </summary>
public static class KommissionierzielFilterWert
{
    public const string AlleZiele = "!(leer)";

    public static (string? Value, bool DroppedTargets) Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return (null, false);

        var parts = input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (!parts.Any(p => string.Equals(p, AlleZiele, StringComparison.OrdinalIgnoreCase)))
            return (input.Trim(), false);

        var dropped = parts.Any(p => !string.Equals(p, AlleZiele, StringComparison.OrdinalIgnoreCase));
        return (AlleZiele, dropped);
    }
}
```

  Controller (alle drei Stellen gleich, Beispiel Account):

```csharp
        var (kommissionierziel, droppedTargets) = KommissionierzielFilterWert.Normalize(vm.DefaultFilterBomKommissionierziel);
        user.DefaultFilterBomKommissionierziel = kommissionierziel;
        ...
        // nach dem bestehenden TempData["SuccessMessage"] = ...:
        if (droppedTargets)
            TempData["WarningMessage"] = "Alle Ziele schließt Einzelziele aus — gespeichert wurde nur 'Alle Ziele'.";
```

  (Erst lesen, ob in diesen POSTs bereits eine `WarningMessage` gesetzt wird; falls ja, anhängen statt überschreiben.)
  Placeholder in Profile/Users-Views: `placeholder="z. B. KA-02 oder !(leer) für alle Ziele"`.
- [ ] **Step 4:** PASS; Voll-Test grün.
- [ ] **Step 5:** Commit: `wip: 2026-09-25-kommissionierung-nur-hauptfa normalisierung komm.-ziel-standardfilter`

---

### Task 6: Stückliste — „Alle Ziele“ + Druck-Leerzustand (nur `Views/Picking/Bom.cshtml`)

**Pflicht zuerst:** Skill `frontend-design` aufrufen. Leitplanken: bestehendes Select2/Bootstrap-Muster, keine neue Bibliothek, Texte deutsch, Kontrast unverändert (vorhandene `alert-warning`-Box).

**Files:** Modify: `IdealAkeWms/Views/Picking/Bom.cshtml` (nur diese Datei; `table-filter.js` NICHT).

Konstante im Script (einmal, nahe `bomDefaultFilterConfig`): `var BOM_ALLE_ZIELE = '!(leer)'; var BOM_ALLE_ZIELE_LABEL = 'Alle Ziele (nur kommissionier-relevant)'; function isAlleZiele(v) { return (v || '').trim().toLowerCase() === BOM_ALLE_ZIELE; }`

- [ ] **Step 1: `bomMatchesFilter`** (Z. ~830) — erste Zeile der Funktion:

```js
                // „Alle Ziele“ (Spec 2026-09-25): Bedeutungswert „Zelle nicht leer“, VOR der Ausschluss-Syntax.
                // val kommt bereits lowercase aus getActiveFilters(). Zwischenstand bis zur Suchsyntax
                // (Backlog 2026-09-23-suchsyntax-spaltenfilter-erweitern) — dort muss diese Stelle aufgehen.
                if (isAlleZiele(val)) return text.trim() !== '';
```

- [ ] **Step 2: `setupKommissionierzielDropdown`** (Z. ~1049-1083):
  - Vor den `values.forEach(...)`-Optionen eine erste Option `value = BOM_ALLE_ZIELE`, `textContent = BOM_ALLE_ZIELE_LABEL` anlegen.
  - Vorbelegung: `pre` so abbilden, dass ein case-insensitiv als `!(leer)` erkannter Teil → genau `[BOM_ALLE_ZIELE]` (Einzelziele verworfen); sonst jeden Teil case-insensitiv auf den exakten Optionswert aus `values` abbilden (ungefundene Teile bleiben wie sie sind):

```js
                var hasAll = pre.some(isAlleZiele);
                pre = hasAll ? [BOM_ALLE_ZIELE] : pre.map(function (p) {
                    var hit = values.find(function (v) { return v.toLowerCase() === p.toLowerCase(); });
                    return hit || p;
                });
```

  - Exklusivität im `change`-Handler: vorherige Auswahl merken (`var lastVals = $sel.val() || [];` nach der Vorbelegung), im Handler:

```js
                    var vals = $sel.val() || [];
                    var addedAll = vals.indexOf(BOM_ALLE_ZIELE) !== -1 && lastVals.indexOf(BOM_ALLE_ZIELE) === -1;
                    if (addedAll && vals.length > 1) vals = [BOM_ALLE_ZIELE];
                    else if (!addedAll && vals.length > 1 && vals.indexOf(BOM_ALLE_ZIELE) !== -1)
                        vals = vals.filter(function (v) { return v !== BOM_ALLE_ZIELE; });
                    if (vals.length !== ($sel.val() || []).length) { $sel.val(vals).trigger('change.select2'); }
                    lastVals = vals;
                    input.value = vals.join(',');
                    // ... bestehende Aufrufe (expandAncestorsOfMatching, updateBomVisibility, Badges, EmptyState) unverändert
```

  - `if (values.length === 0) $sel.prop('disabled', true);` bleibt (H7: gesperrt, aber Vorauswahl sichtbar).
- [ ] **Step 3: `renderDefaultFilterBadges`** — für `cfg.key === 'kommissionieren' && isAlleZiele(val)` Label `'Gefiltert: ' + BOM_ALLE_ZIELE_LABEL`, sonst unverändert.
- [ ] **Step 4: `checkKommissionierzielEmptyState`** — Text bei `isAlleZiele(val)`: `'Standardfilter „' + BOM_ALLE_ZIELE_LABEL + '“ aktiv — keine Treffer in dieser Stückliste.'`, sonst unverändert.
- [ ] **Step 5: Druck-Handler `btnPrintBom`** (Z. ~1167-1211):
  - Nach dem Sammeln von `visiblePositions`: wenn `visiblePositions.length === 0`, dann Box `#bomKzEmptyState` zeigen (`bomKzEmptyStateText.textContent = 'Keine sichtbaren Positionen — nichts zu drucken.'; box.hidden = false;`) und `return;` — kein `window.open`. Existiert die Box nicht (Flachmodus rendert sie evtl. nicht — prüfen, `Bom.cshtml` Z. 136), trotzdem `return;` und stattdessen `btnPrintBom` kurz mit `title`/`aria-live`-Hinweis? → Erst prüfen, ob `#bomKzEmptyState` unabhängig vom Modus gerendert wird; falls nur hierarchisch, die Box-Markup-Bedingung so erweitern, dass sie in beiden Modi existiert (Hinweis ist modusunabhängig).
  - `filterParts`: `if (col === 'kommissionieren' && isAlleZiele(filters[col])) filterParts.push('Komm.-Ziel=' + BOM_ALLE_ZIELE_LABEL); else filterParts.push(...bestehend...)`.
- [ ] **Step 6:** `dotnet build IdealAkeWms.slnx` (Razor kompiliert) + `dotnet test IdealAkeWms.Tests` grün. Manuelle Sichtprüfung nicht möglich → in TS-79 abgedeckt.
- [ ] **Step 7:** Commit: `wip: 2026-09-25-kommissionierung-nur-hauptfa stueckliste alle ziele + druck-leerzustand`

---

### Task 7: Version, Changelog, Hilfe, Testszenarien

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/AppVersion.cs` → `1.46.0`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` — neuer Eintrag v1.46.0 (Datum 25.09.2026, Stil der Nachbar-Einträge) + im v1.38.0-Eintrag Überschrift (Z. ~180) „…, Freigabe-Kaskade“ und Bullet (Z. ~203-207) als zurückgenommen kennzeichnen (z. B. `<del>` + „(zurückgenommen in v1.46.0 — Kommissionierung nur am HauptFA)“)
- Modify: `IdealAkeWms/Views/Help/Index.cshtml` Abschnitt „Leitstand (Kommissionier-Freigabe)“ (ab Z. ~695) + Stücklisten-/Picking-Abschnitt: „Freigabe und Stückliste nur an der HauptFA-Zeile“ + „Alle Ziele“ (inkl. Profil-Eingabe `!(leer)`)
- Modify: `docs/TESTSZENARIEN.md` — neues Kapitel `## TS-79 — IDEAL: Kommissionierung nur am HauptFA — „Alle Ziele“, Rückbau Freigabe-Kaskade (v1.46.0)` nach TS-78 mit Szenarien TS-79.1 ff. aus dem Spec-Abschnitt „Test-Szenarien“ (vollständig: Vorbedingungen, Schritte, Erwartung, Negativfälle; inkl. einmaligem SQL-Lauf als Vorbedingung von „Warteschlange bereinigt“, Hinweis Warteschlangen-Gruppierung, erwartetes Baumverhalten „kein Fehler“, `!(LEER)` ohne Hinweis). TS-73: Titel Z. ~7726, Z. ~7753, TS-73.11–13, Z. ~7895, Z. ~7940–7952 als „zurückgebaut in v1.46.0, siehe TS-79“ kennzeichnen (nicht löschen; Vorbild TS-71-Vermerk suchen: `grep -n "zurückgebaut\|Rückbau" docs/TESTSZENARIEN.md`). Schlusszeile (letzte Zeile) um „+ Kommissionierung nur am HauptFA (v1.46.0, TS-79)“ ergänzen.
- [ ] **Step 1-3:** wie oben umsetzen.
- [ ] **Step 4:** `dotnet build IdealAkeWms.slnx` + `dotnet test` (beide Projekte) grün.
- [ ] **Step 5:** Commit: `wip: 2026-09-25-kommissionierung-nur-hauptfa feature-complete (v1.46.0)`

---

## Nach den Tasks (Orchestrator, Hauptcheckout — NICHT die Implementer)

- Code-Review über den Gesamt-Diff, `superpowers:verification-before-completion`, qa-agent.
- Brain: `fallstricke.md` (Platzhalter in Komm.-Ziel-Zelle; `IsHauptFa` als einzige Sub-FA-Definition), `2026-09-10-fa-liste-ausbau-matchcode-spec` Block 4 als zurückgebaut markieren, `testszenarien-index.md` (TS-79), `changelog/2026-09-25-v1-46-0-kommissionierung-nur-hauptfa.md`, `feature-map.md`, Aufgabe/Spec-Status.
