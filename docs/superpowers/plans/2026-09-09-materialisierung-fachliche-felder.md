# Materialisierung fachliche Felder (K1/K2) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Die IDEAL-Materialisierung schreibt die fachlichen Zeilenfelder (Werkbank, Lack-Flag) und die FA-Liste zeigt die Auftragsdaten je HauptFA — und dabei wird der im UAT gemeldete Fehler „PickingStatus-Zeile fehlt" mitbehoben.

**Architecture:** Drei Klassen von Feldern. **K1** (Kunde, Termine, Prio, AB-Nr., Montage-Abt.) haengt am HauptFA und wird **nur angezeigt**, nie materialisiert — die Gruppen-Kopfzeile liest sie gebuendelt aus `FaHierarchyOrderInfo`. **K2** (Werkbank, abgeleitetes `HasCoatingParts`) haengt am Sub-FA und wird im Materialisierungs-Sync in **Anlege- und Update-Zweig** geschrieben. **K3** (Komm.-Status) bleibt leer. Voraussetzung fuer K2 ist der Eager-Create der beiden 1:1-Statuszeilen — genau die Luecke, die den UAT-Fehler ausloest.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10, Windows-Service, Bootstrap 5, xUnit/FluentAssertions/Moq/EF-InMemory.

**Spec:** `secondbrain/specs/freigegeben/2026-08-20-materialisierung-fachliche-felder-spec.md` (Hauptcheckout — im Worktree nicht sichtbar, sparse-checkout). Aufgabe: `secondbrain/aufgaben/2026-09-09-materialisierung-fachliche-felder-umsetzung.md`. Bug: `secondbrain/bugs/2026-09-09-materialisierung-ohne-statuszeilen-bug.md`.

## Global Constraints

- **Master-Gate:** Der gesamte Materialisierungspfad laeuft **nur** bei `ProduktionsauftragHierarchisch = true`. Bei `false` aendert sich nichts (F4). Der Web-Teil verzweigt am bereits vorhandenen `hierarchical`-Flag in `ProductionOrdersController.Index`.
- **F3 — kein `SetValues`, kein `_ctx.Update(entity)`.** Jedes Feld wird **einzeln** zugewiesen, im Muster der bestehenden `Quantity`/`ArticleNumber`-Zeilen.
- **Z1-Ausnahmen sind genau zwei:** `ProductionOrder.ProductionWorkplaceId` und `ProductionOrderPickingStatus.HasCoatingParts`. **Alle** uebrigen app-verwalteten Felder (`IsDone`, `IsDonePicking`, `PickingPriority`, `AssignedPicker`, `HasGlass`, `HasExternalPurchase`, `IsReleasedForPicking`, `BdeStatus.IsDoneBde`, Storno, ExtraInfo) bleiben tabu.
- **Keine Migration.** Es wird keine Spalte hinzugefuegt. Wer im Lauf zu dem Schluss kommt, eine zu brauchen, hat den Zuschnitt verlassen — stoppen und melden.
- **Audit:** Jede vom Sync geschriebene Zeile setzt `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` auf `ServiceUser` (bestehende Konstante im Service).
- **ADR 0010:** `ISyncLogger` bleibt **letzter** Konstruktor-Parameter. Counts-Keys sind deutsch.
- **DryRun:** Jeder neue Schreibpfad respektiert `dryRun` — zaehlen ja, schreiben nein.
- **Umfang der Anzeige: NUR `/ProductionOrders`.** Leitstand, Tracking, Picking, Arbeitsvorrat, FaCompletion bleiben unveraendert und zeigen K1 fuer IDEAL weiterhin leer (bewusste, dokumentierte Einschraenkung, B5).
- **Zwei Registrierungen:** Wird eine Spalte in einer Liste ergaenzt, muss sie in `ColumnDefinitions.<View>` **und** im Inline-`<script id="column-config">` stehen (fallstricke §10). In diesem Plan entsteht **keine** neue Spalte — die K1-Felder stehen in der Gruppen-Kopfzeile, nicht in einer `<th>`-Spalte.
- **Version:** 1.36.0 → **1.37.0** in **beiden** `AppVersion.cs`.
- **Sprachregel:** Code englisch, UI-Texte deutsch.

## Pre-Flight-Scan (vor Task 1 durchgefuehrt)

| Paar / Task | Gemeinsame Datei oder Schnittstelle | Befund |
|---|---|---|
| T1 ↔ T2 ↔ T3 ↔ T4 | `FaMaterializationSyncService.cs` | **Streng sequenziell**, nie parallel. Reihenfolge zwingend: T1 legt die Statuszeilen an, T4 schreibt in sie hinein (`SetCoatingPartsAsync` aktualisiert nur bestehende Zeilen). |
| T1 ↔ T3 ↔ T4 | die **drei** `FinishSuccessAsync`-Aufrufe (Skip/DryRun/Erfolg) | Driftstelle: jede Task ergaenzt eigene Counts-Keys. Endreview prueft, dass **alle** Keys in **allen drei** Aufrufen stehen. |
| T2 ↔ T1 | `srcByKey` (lokale Variable) | T1 nutzt `srcByKey.Keys`; T2 aendert nur den Record-Inhalt, nicht den Schluessel. Kein Konflikt. |
| T2 | `MaterializationSourceOrder`-Signatur | Bricht `FaMaterializationPlannerTests`. **In derselben Task** mitziehen, sonst rot. |
| T6 → T8 | `GetByHauptFaKeysAsync` | Reihenfolge T6 vor T8. |
| T7 ↔ T8 | Repository vs. Controller | Getrennte Dateien, kein Konflikt; T7 wirkt zusaetzlich auf den Leitstand. |
| T8 → T9 | `ProductionOrderListGroup` | Reihenfolge T8 vor T9. |
| T5, T10 | — | Unabhaengig. |
| Selbstpruefung T1 | Testklasse | `FaMaterializationSyncServiceTests` mit `NewContext()`/`NewSut()` **erst pruefen**; existiert sie nicht, im Muster der uebrigen Service-Tests neu anlegen und die Helfer definieren. |
| Selbstpruefung T3 | `ApplyWorkplace`-Signatur | Anonyme Typen sind nicht uebergabefaehig → eigener Record `WorkplaceRef` (unten definiert). |
| Selbstpruefung T4 | Konstruktor | `IProductionOrderPickingStatusRepository _pickingStatus` wird ergaenzt — **vor** `ISyncLogger` (ADR 0010). |
| Selbstpruefung T7 | Feldname des DbContext | `ProductionOrderRepository` arbeitet mit `_dbSet`; der Name des Context-Feldes ist vor der Subquery am Code zu pruefen und zu verwenden, nicht zu raten. |

**Rulings vor dem Start:**
1. **Eager-Create umfasst beide Statustabellen** (`PickingStatus` **und** `BdeStatus`), obwohl F6 nur `PickingStatus` nennt — der UAT-Befund zeigt dieselbe Luecke bei `BdeStatus`, und der AKE-Sync legt beide als Paar an. Kosten falls falsch: eine ueberfluessige leere Zeile je Sub-FA.
2. **Werkbank-Lookup im Service ueber den `ApplicationDbContext`, nicht ueber eine neue `GetByNamesAsync`-Repository-Methode** (S2 schlug letzteres vor). Grund: `FaMaterializationSyncService` injiziert heute **kein** Repository, sondern arbeitet direkt am Context; eine Methode nur fuer diesen Aufrufer waere ein Fremdkoerper. S2s eigentliche Forderung — **eine** Abfrage je Lauf, case-insensitiv, getrimmt, Mehrfachtreffer melden — wird vollstaendig erfuellt. Kosten falls falsch: der Lookup wandert spaeter ins Repository, ein Methodenumzug.
3. **Kombigeraet-Meldung ohne Mail**, nur ins Aktivitaets-Protokoll: `FaHierarchySyncService` hat keinen Mail-Weg (nur `ISyncErrorNotifier` fuer zwei Fehlerfaelle). S1 nennt das Protokoll ausdruecklich als ausreichende Alternative. Kosten falls falsch: ein Digest-Versand wird nachgeruestet.
4. **`IProductionOrderPickingStatusRepository` wird im Service-Host registriert** (Task 4). Das ist kein Praezedenzfall — der Host registriert bereits `IStockMovementRepository`, `ISageBookingQueueRepository` und `IWarehouseRequisitionRepository`.

## File Structure

**Service (`IDEALAKEWMSService/`)**
| Datei | Verantwortung |
|---|---|
| `Services/FaMaterializationPlanner.cs` | reine Regeln; `MaterializationSourceOrder` waechst um zwei Felder |
| `Services/FaMaterializationCoating.cs` (NEU) | reiner Helper: Lack-Ableitung „selbst oder direktes Kind" |
| `Services/FaMaterializationSyncService.cs` | Eager-Create, Werkbank-Zuweisung, Lack-Flag, Meldungen |
| `Common/IUnknownWorkplaceState.cs` (NEU) | Singleton: welche unbekannten Arbeitsbereiche wurden zuletzt gemeldet (Mail nur bei Aenderung) |
| `Services/FaHierarchySyncService.cs` | Kombigeraet-Erkennung, einmal je Lauf |
| `Program.cs` | zwei neue Registrierungen |

**Web (`IdealAkeWms/`)**
| Datei | Verantwortung |
|---|---|
| `Data/Repositories/IFaHierarchyOrderInfoRepository.cs` + Impl | neue Mengen-Methode `GetByHauptFaKeysAsync` |
| `Data/Repositories/ProductionOrderRepository.cs` | Kunde-Freitext **und** Kunde-Spaltenfilter im hierarchischen Modus per Subquery |
| `Models/ViewModels/ProductionOrderListViewModel.cs` | `ProductionOrderListGroup` bekommt die K1-Felder |
| `Controllers/ProductionOrdersController.cs` | K1 laden, Termin-Kaskade auf Gruppenwert, Beschichtungstermin |
| `Views/ProductionOrders/Index.cshtml` | Gruppen-Kopfzeile + Mehrdeutigkeits-Tabelle |
| `Views/Help/Changelog.cshtml`, `Views/Help/Index.cshtml` | Anwender-Doku |
| beide `AppVersion.cs` | 1.37.0 |
| `docs/TESTSZENARIEN.md` | neues Kapitel TS-71 |

---

### Task 1: Eager-Create der beiden Statuszeilen (UAT-Fehler H0 + Spec-F6)

Dies ist der gemeldete Fehler **und** die Voraussetzung fuer Task 4. Zuerst, weil ohne ihn jeder Schreibversuch auf `PickingStatus` ins Leere laeuft.

**Files:**
- Modify: `IDEALAKEWMSService/Services/FaMaterializationSyncService.cs`
- Test: `IDEALAKEWMSService.Tests/Services/FaMaterializationSyncServiceTests.cs` (existiert; falls nicht, neu anlegen im Muster der uebrigen Service-Tests mit EF-InMemory)

**Interfaces:**
- Consumes: `ApplicationDbContext` (bereits im Service), `srcByKey` (lokale Variable im `RunAsync`)
- Produces: Counts-Key `statuszeilen_ergaenzt` im `FaMaterialization`-SyncLog

- [ ] **Step 1: Failing Test — Anlege-Pfad erzeugt beide Statuszeilen**

```csharp
[Fact]
public async Task RunAsync_NewSubFa_CreatesPickingStatusAndBdeStatusRows()
{
    using var ctx = NewContext();                       // EF-InMemory-Helper der Testklasse
    ctx.FaHierarchyNodes.Add(new FaHierarchyNode { HauptFA = 1000, SubFA = 1001, VaterFA = null, Sollmenge = 1 });
    await ctx.SaveChangesAsync();

    var sut = NewSut(ctx);                              // Master true, dryRun false
    await sut.RunAsync(dryRun: false);

    var order = await ctx.ProductionOrders.SingleAsync(o => o.SubOrderNumber == "1001");
    (await ctx.ProductionOrderPickingStatuses.CountAsync(s => s.ProductionOrderId == order.Id)).Should().Be(1);
    (await ctx.ProductionOrderBdeStatuses.CountAsync(s => s.ProductionOrderId == order.Id)).Should().Be(1);
}
```

- [ ] **Step 2: Failing Test — Bestandszeile ohne Statuszeile wird nachgezogen (der gemeldete Fehler)**

```csharp
[Fact]
public async Task RunAsync_ExistingSubFaWithoutStatusRows_BackfillsThem()
{
    using var ctx = NewContext();
    ctx.FaHierarchyNodes.Add(new FaHierarchyNode { HauptFA = 1000, SubFA = 1001, VaterFA = null, Sollmenge = 1 });
    ctx.ProductionOrders.Add(new ProductionOrder
    {
        OrderNumber = "1000", SubOrderNumber = "1001", Quantity = 1,
        CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test"
    });                                                  // bewusst OHNE Statuszeilen
    await ctx.SaveChangesAsync();

    await NewSut(ctx).RunAsync(dryRun: false);

    var order = await ctx.ProductionOrders.SingleAsync(o => o.SubOrderNumber == "1001");
    (await ctx.ProductionOrderPickingStatuses.AnyAsync(s => s.ProductionOrderId == order.Id)).Should().BeTrue();
    (await ctx.ProductionOrderBdeStatuses.AnyAsync(s => s.ProductionOrderId == order.Id)).Should().BeTrue();
}
```

- [ ] **Step 3: Failing Test — DryRun schreibt keine Statuszeilen**

```csharp
[Fact]
public async Task RunAsync_DryRun_DoesNotCreateStatusRows()
{
    using var ctx = NewContext();
    ctx.FaHierarchyNodes.Add(new FaHierarchyNode { HauptFA = 1000, SubFA = 1001, VaterFA = null, Sollmenge = 1 });
    await ctx.SaveChangesAsync();

    await NewSut(ctx).RunAsync(dryRun: true);

    (await ctx.ProductionOrderPickingStatuses.CountAsync()).Should().Be(0);
    (await ctx.ProductionOrderBdeStatuses.CountAsync()).Should().Be(0);
}
```

- [ ] **Step 4: Tests laufen lassen — muessen FAILEN**

Run: `dotnet test IDEALAKEWMSService.Tests --filter FaMaterializationSyncServiceTests`
Expected: die drei neuen Tests rot (keine Statuszeilen vorhanden).

- [ ] **Step 5: Implementieren — Ensure-Methode direkt nach dem bestehenden `SaveChangesAsync`**

In `RunAsync`, **unmittelbar nach** `await _ctx.SaveChangesAsync(ct);` (der Aufruf, der heute Anlegen und Update abschliesst — dort haben neue Zeilen ihre `Id`) und **vor** `SendMissingDigestAsync`:

```csharp
// F6 / UAT-Befund H0: Die beiden 1:1-Satelliten-Zeilen existieren fuer materialisierte
// Sub-FAs sonst NIE. Der AKE-Sync legt sie eager an (SageImportService); die
// Materialisierung tat das bisher nicht — Folge: Freigabe im Leitstand scheitert mit
// "PickingStatus-Zeile fehlt", BDE-Rueckmeldung und Kaskade laufen ins Leere.
// Anlege- UND Update-Pfad, damit auch der Bestand nachgezogen wird (F6).
var statusRowsCreated = 0;
if (!dryRun)
    statusRowsCreated = await EnsureStatusRowsAsync(srcByKey.Keys, now, ct);
```

Neue private Methode am Ende der Klasse:

```csharp
/// <summary>
/// Stellt fuer alle materialisierten Sub-FAs die beiden 1:1-Statuszeilen sicher
/// (<see cref="ProductionOrderPickingStatus"/>, <see cref="ProductionOrderBdeStatus"/>).
/// Idempotent — legt nur an, was fehlt; bestehende Werte werden nie angefasst
/// (Muster: WHEN NOT MATCHED BY TARGET im AKE-Eager-Create).
/// </summary>
private async Task<int> EnsureStatusRowsAsync(
    IEnumerable<string> subOrderNumbers, DateTime now, CancellationToken ct)
{
    var keys = subOrderNumbers.ToList();
    if (keys.Count == 0) return 0;

    var ids = await _ctx.ProductionOrders
        .Where(o => o.SubOrderNumber != null && keys.Contains(o.SubOrderNumber))
        .Select(o => o.Id)
        .ToListAsync(ct);
    if (ids.Count == 0) return 0;

    var withPicking = await _ctx.ProductionOrderPickingStatuses
        .Where(s => ids.Contains(s.ProductionOrderId))
        .Select(s => s.ProductionOrderId).ToListAsync(ct);
    var withBde = await _ctx.ProductionOrderBdeStatuses
        .Where(s => ids.Contains(s.ProductionOrderId))
        .Select(s => s.ProductionOrderId).ToListAsync(ct);

    var created = 0;
    foreach (var id in ids.Except(withPicking))
    {
        _ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus
        {
            ProductionOrderId = id,
            CreatedAt = now, CreatedBy = ServiceUser, CreatedByWindows = ServiceUser
        });
        created++;
    }
    foreach (var id in ids.Except(withBde))
    {
        _ctx.ProductionOrderBdeStatuses.Add(new ProductionOrderBdeStatus
        {
            ProductionOrderId = id,
            CreatedAt = now, CreatedBy = ServiceUser, CreatedByWindows = ServiceUser
        });
        created++;
    }

    if (created > 0) await _ctx.SaveChangesAsync(ct);
    return created;
}
```

Alle **drei** `FinishSuccessAsync`-Aufrufe (Skip-, DryRun-, Erfolgspfad) bekommen den zusaetzlichen Counts-Key `["statuszeilen_ergaenzt"] = statusRowsCreated` (im Skip-Pfad `0`, im DryRun-Pfad `0`). Im Erfolgs-Log-Eintrag die Zahl mitfuehren.

- [ ] **Step 6: Tests laufen lassen — muessen GRUEN sein**

Run: `dotnet test IDEALAKEWMSService.Tests --filter FaMaterializationSyncServiceTests`
Expected: alle gruen, inkl. der vorbestehenden Tests.

- [ ] **Step 7: Commit**

```bash
git add IDEALAKEWMSService/Services/FaMaterializationSyncService.cs IDEALAKEWMSService.Tests/Services/FaMaterializationSyncServiceTests.cs
git commit -m "fix: Materialisierung legt PickingStatus- und BdeStatus-Zeile eager an (UAT-Befund H0, Spec F6)"
```

---

### Task 2: Planner erweitern + reine Lack-Ableitung

**Files:**
- Modify: `IDEALAKEWMSService/Services/FaMaterializationPlanner.cs`
- Create: `IDEALAKEWMSService/Services/FaMaterializationCoating.cs`
- Modify: `IDEALAKEWMSService/Services/FaMaterializationSyncService.cs` (Quellaufbau)
- Test: `IDEALAKEWMSService.Tests/Services/FaMaterializationCoatingTests.cs` (neu), `FaMaterializationPlannerTests.cs` (existiert)

**Interfaces:**
- Produces: `MaterializationSourceOrder(string SubOrderNumber, string OrderNumber, string? ParentSubOrderNumber, string? Arbeitsbereich, bool HasCoatingParts)` — die zwei neuen Felder haben **keine** Defaultwerte, damit jeder Erzeuger sie bewusst setzt.
- Produces: `FaMaterializationCoating.Derive(IReadOnlyCollection<FaHierarchyNode> nodes) -> Dictionary<string,bool>` (Schluessel `SubFA.ToString()`)

- [ ] **Step 1: Failing Test — Ableitung „selbst oder direktes Kind"**

```csharp
public class FaMaterializationCoatingTests
{
    private static FaHierarchyNode N(int hauptFa, int subFa, int? vaterFa, bool beschichtet)
        => new() { HauptFA = hauptFa, SubFA = subFa, VaterFA = vaterFa, Beschichtet = beschichtet };

    [Fact]
    public void Derive_NodeItselfCoated_IsTrue()
        => FaMaterializationCoating.Derive(new[] { N(1, 10, null, true) })["10"].Should().BeTrue();

    [Fact]
    public void Derive_DirectChildCoated_IsTrue()
    {
        var map = FaMaterializationCoating.Derive(new[] { N(1, 10, null, false), N(1, 0, 10, true) });
        map["10"].Should().BeTrue();
    }

    [Fact]
    public void Derive_OnlyGrandchildCoated_IsFalse()
    {
        // 10 -> 20 -> Blatt(beschichtet). Fuer 10 zaehlt nur die DIREKTE Kindebene.
        var map = FaMaterializationCoating.Derive(new[]
        {
            N(1, 10, null, false), N(1, 20, 10, false), N(1, 0, 20, true)
        });
        map["10"].Should().BeFalse();
        map["20"].Should().BeTrue();
    }

    [Fact]
    public void Derive_NothingCoated_IsFalse()
        => FaMaterializationCoating.Derive(new[] { N(1, 10, null, false), N(1, 0, 10, false) })["10"].Should().BeFalse();
}
```

- [ ] **Step 2: Test laufen lassen — muss FAILEN**

Run: `dotnet test IDEALAKEWMSService.Tests --filter FaMaterializationCoatingTests`
Expected: Kompilierfehler „FaMaterializationCoating nicht gefunden".

- [ ] **Step 3: Helper implementieren**

```csharp
using IdealAkeWms.Models;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Reine Ableitung des Lackier-Flags aus der FA-Struktur (S8: DB-frei, unit-testbar).
/// Regel (Spec K2): Ein Sub-FA gilt als beschichtet, wenn er SELBST beschichtet ist
/// oder mindestens ein DIREKTES Kind (VaterFA == SubFA) beschichtet ist. Enkel zaehlen
/// bewusst nicht — sie tragen ihr Flag an ihrem eigenen Sub-FA.
/// Ersetzt fuer IDEAL die AKE-Heuristik (CoatingDetectionService, per Klasse-D-Gate aus).
/// </summary>
public static class FaMaterializationCoating
{
    public static Dictionary<string, bool> Derive(IReadOnlyCollection<FaHierarchyNode> nodes)
    {
        var coatedParents = new HashSet<int>();
        foreach (var n in nodes)
            if (n.Beschichtet && n.VaterFA.HasValue)
                coatedParents.Add(n.VaterFA.Value);

        var result = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var n in nodes)
        {
            if (n.SubFA == 0) continue;                 // Blaetter werden nicht materialisiert
            result[n.SubFA.ToString()] = n.Beschichtet || coatedParents.Contains(n.SubFA);
        }
        return result;
    }
}
```

- [ ] **Step 4: Record erweitern und Quellaufbau anpassen**

`FaMaterializationPlanner.cs`:
```csharp
public sealed record MaterializationSourceOrder(
    string SubOrderNumber, string OrderNumber, string? ParentSubOrderNumber,
    string? Arbeitsbereich, bool HasCoatingParts);
```

`FaMaterializationSyncService.RunAsync`, Quellaufbau (heute Z56-63):
```csharp
var coating = FaMaterializationCoating.Derive(nodes);

var source = nodes
    .Where(n => n.SubFA != 0)
    .Select(n => new MaterializationSourceOrder(
        n.SubFA.ToString(),
        n.HauptFA.ToString(),
        n.VaterFA.HasValue ? n.VaterFA.Value.ToString() : null,
        string.IsNullOrWhiteSpace(n.Arbeitsbereich) ? null : n.Arbeitsbereich.Trim(),
        coating.GetValueOrDefault(n.SubFA.ToString())))
    .ToList();
```

Bestehende `FaMaterializationPlannerTests` an die neue Signatur anpassen (die zwei neuen Felder mit `null`/`false` fuellen — die Plan-Regeln haengen nicht daran).

- [ ] **Step 5: Tests laufen lassen — muessen GRUEN sein**

Run: `dotnet test IDEALAKEWMSService.Tests`
Expected: alle gruen.

- [ ] **Step 6: Commit**

```bash
git add IDEALAKEWMSService/Services/FaMaterializationCoating.cs IDEALAKEWMSService/Services/FaMaterializationPlanner.cs IDEALAKEWMSService/Services/FaMaterializationSyncService.cs IDEALAKEWMSService.Tests
git commit -m "feat: Lack-Ableitung als reiner Helper + Arbeitsbereich im Quell-Record"
```

---

### Task 3: Werkbank-Zuweisung (Variante B) mit Meldungen

**Files:**
- Create: `IDEALAKEWMSService/Common/IUnknownWorkplaceState.cs`
- Modify: `IDEALAKEWMSService/Services/FaMaterializationSyncService.cs`
- Modify: `IDEALAKEWMSService/Program.cs`
- Test: `IDEALAKEWMSService.Tests/Services/FaMaterializationWorkplaceTests.cs` (neu)

**Interfaces:**
- Consumes: `MaterializationSourceOrder.Arbeitsbereich` (Task 2)
- Produces: Counts-Keys `werkbank_gesetzt`, `werkbank_abweichend`, `arbeitsbereich_unbekannt`, `arbeitsbereich_mehrdeutig`

**Regeln (aus der Freigabe, nicht verhandelbar):**
- Sage ist fuehrend: ein abweichender Bestandswert wird **ueberschrieben**.
- Die Meldung heisst **„Werkbank weicht vom Quellwert ab"** — sie darf **nicht** behaupten, jemand habe manuell zugewiesen (B4).
- Unbekannter Arbeitsbereich: **melden, nicht anlegen**. Zuweisung unterbleibt, Bestandswert bleibt unangetastet.
- Name-Match **case-insensitiv und getrimmt**. Bei **mehreren** Treffern: **keine** Zuweisung, eigener Meldefall (S2) — nie „erster Treffer gewinnt".
- Log bei jedem Lauf, **Mail nur wenn sich die Menge der unbekannten Namen aendert** (S1).

- [ ] **Step 1: Failing Tests**

```csharp
[Fact]
public async Task RunAsync_KnownArbeitsbereich_AssignsWorkplace() { /* Node mit Arbeitsbereich "S-01",
    ProductionWorkplace "S-01" existiert -> ProductionWorkplaceId gesetzt */ }

[Fact]
public async Task RunAsync_ArbeitsbereichMatchesCaseInsensitiveAndTrimmed_AssignsWorkplace() { /* " s-01 " -> "S-01" */ }

[Fact]
public async Task RunAsync_UnknownArbeitsbereich_LeavesWorkplaceNullAndReports() { /* kein Stammsatz ->
    ProductionWorkplaceId bleibt null, Counts-Key arbeitsbereich_unbekannt = 1 */ }

[Fact]
public async Task RunAsync_AmbiguousWorkplaceName_DoesNotAssign() { /* zwei ProductionWorkplace mit Namen "S-01"
    -> keine Zuweisung, Counts-Key arbeitsbereich_mehrdeutig = 1 */ }

[Fact]
public async Task RunAsync_ExistingDifferentWorkplace_IsOverwrittenAndCounted() { /* Bestandszeile zeigt K-02,
    Quelle sagt S-01 -> danach S-01, Counts-Key werkbank_abweichend = 1 */ }

[Fact]
public async Task RunAsync_DryRun_DoesNotChangeWorkplace() { }
```

- [ ] **Step 2: Tests laufen lassen — muessen FAILEN**

Run: `dotnet test IDEALAKEWMSService.Tests --filter FaMaterializationWorkplaceTests`

- [ ] **Step 3: Zustandshalter fuer die Mail-Entprellung**

```csharp
namespace IDEALAKEWMSService.Common;

/// <summary>
/// Haelt die zuletzt gemeldeten unbekannten Arbeitsbereiche, damit die Sammelmail nur bei
/// AENDERUNG rausgeht (S1: sonst 96 identische Mails am Tag bei 15-Minuten-Takt).
/// Bewusst nur im Speicher — nach einem Dienst-Neustart einmal zu viel zu melden ist harmlos.
/// </summary>
public interface IUnknownWorkplaceState
{
    /// <summary>true, wenn sich die Menge gegenueber der letzten Meldung geaendert hat (dann merken).</summary>
    bool HasChanged(IReadOnlyCollection<string> unknownNames);
}

public sealed class UnknownWorkplaceState : IUnknownWorkplaceState
{
    private readonly object _gate = new();
    private HashSet<string> _last = new(StringComparer.OrdinalIgnoreCase);

    public bool HasChanged(IReadOnlyCollection<string> unknownNames)
    {
        var now = new HashSet<string>(unknownNames, StringComparer.OrdinalIgnoreCase);
        lock (_gate)
        {
            if (_last.SetEquals(now)) return false;
            _last = now;
            return true;
        }
    }
}
```

`Program.cs` (bei den uebrigen Singletons, neben `IHierarchicalModeReader`):
```csharp
builder.Services.AddSingleton<IDEALAKEWMSService.Common.IUnknownWorkplaceState,
    IDEALAKEWMSService.Common.UnknownWorkplaceState>();
```

- [ ] **Step 4: Zuweisung im Sync implementieren**

Konstruktor bekommt `IUnknownWorkplaceState unknownWorkplaces` **vor** `ISyncLogger`.

Vor der Zuweisungsschleife **eine** Abfrage je Lauf (kein N+1):

```csharp
/// <summary>Werkbank-Kurzform fuer den Name-Match (anonyme Typen sind nicht uebergabefaehig).</summary>
private sealed record WorkplaceRef(int Id, string Name);
```

```csharp
// S2: Name-Match case-insensitiv + getrimmt. ProductionWorkplace.Name hat KEINEN
// Unique-Index — mehrdeutige Namen sind moeglich und werden gemeldet, nicht geraten.
var workplaceGroups = (await _ctx.ProductionWorkplaces.AsNoTracking()
        .Select(w => new WorkplaceRef(w.Id, w.Name))
        .ToListAsync(ct))
    .GroupBy(w => (w.Name ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
```

In der bestehenden Schleife ueber `orders` (Update-Zweig) **und** fuer die neu angelegten Zeilen: die Werkbank aus `srcByKey[key].Arbeitsbereich` aufloesen und **einzeln** zuweisen (F3). Meldefaelle in drei Mengen sammeln (`unknownNames`, `ambiguousNames`, `deviations`).

Zuweisungsregel als eigene private Methode, damit Anlege- und Update-Zweig **denselben** Code nutzen (F1 — der Bestandsunterschied Anlegen/Update ist die Ursache des ganzen Befunds und darf sich nicht wiederholen):

```csharp
/// <returns>true, wenn der Wert geaendert wurde.</returns>
private bool ApplyWorkplace(ProductionOrder order, string? arbeitsbereich,
    IReadOnlyDictionary<string, List<WorkplaceRef>> groups,
    ISet<string> unknown, ISet<string> ambiguous, IList<string> deviations)
{
    if (string.IsNullOrWhiteSpace(arbeitsbereich)) return false;
    var name = arbeitsbereich.Trim();

    if (!groups.TryGetValue(name, out var matches)) { unknown.Add(name); return false; }
    if (matches.Count > 1) { ambiguous.Add(name); return false; }

    var targetId = matches[0].Id;
    if (order.ProductionWorkplaceId == targetId) return false;

    // B4: Der Sync kann NICHT unterscheiden, ob der Bestandswert von Hand gesetzt oder in
    // Sage umgeplant wurde. Die Meldung bleibt deshalb bewusst unscharf.
    if (order.ProductionWorkplaceId.HasValue)
        deviations.Add($"Sub-FA {order.SubOrderNumber}: Werkbank weicht vom Quellwert ab " +
                       $"(bisher Id {order.ProductionWorkplaceId}, Quelle '{name}')");

    order.ProductionWorkplaceId = targetId;
    return true;
}
```

Nach der Schleife (nur wenn `!dryRun` geschrieben wurde) melden:

```csharp
if (unknown.Count > 0)
{
    var list = string.Join(", ", unknown.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    await run.LogWarningAsync(
        $"Unbekannte Arbeitsbereiche ohne Werkbank-Stammsatz ({unknown.Count}): {list}. " +
        "Keine Zuweisung — Werkbank einmalig anlegen, der naechste Lauf fuellt sie.", ct: ct);
    if (_unknownWorkplaces.HasChanged(unknown.ToList()))
        await SendUnknownWorkplaceDigestAsync(unknown, ct);   // Muster SendMissingDigestAsync
}
if (ambiguous.Count > 0)
    await run.LogWarningAsync($"Arbeitsbereich-Name mehrfach in den Werkbank-Stammdaten " +
        $"({ambiguous.Count}): {string.Join(", ", ambiguous)}. Keine Zuweisung.", ct: ct);
foreach (var d in deviations)
    await run.LogWarningAsync(d, ct: ct);
```

`SendUnknownWorkplaceDigestAsync` wird nach dem Vorbild von `SendMissingDigestAsync` gebaut (gleiches Feature-Gate `ErrorNotification:Enabled`, gleiche Empfaengerliste, `IMailService.SendAsync`).

Klassenkommentar (Z21-22) korrigieren: `Workplace` faellt aus der Liste der app-verwalteten Felder heraus, mit Begruendung und Verweis auf Variante B.

- [ ] **Step 5: Tests gruen + Commit**

Run: `dotnet test IDEALAKEWMSService.Tests`

```bash
git commit -am "feat: Werkbank aus Arbeitsbereich (Variante B) mit Meldung bei Abweichung, unbekanntem und mehrdeutigem Namen"
```

---

### Task 4: `HasCoatingParts` schreiben

**Files:**
- Modify: `IDEALAKEWMSService/Services/FaMaterializationSyncService.cs`
- Modify: `IDEALAKEWMSService/Program.cs`
- Test: `IDEALAKEWMSService.Tests/Services/FaMaterializationCoatingWriteTests.cs` (neu)

**Interfaces:**
- Consumes: `FaMaterializationCoating.Derive` (Task 2), Statuszeilen aus Task 1
- Consumes: `IProductionOrderPickingStatusRepository.SetCoatingPartsAsync(Dictionary<int,bool>)` — vorhanden, setzt bei `false` auch `IsCoatingDone` zurueck (Fallstrick #11)
- Produces: Counts-Key `lackflag_gesetzt`

Das Repository wird injiziert und in `Program.cs` registriert — der Service-Host registriert bereits mehrere Web-Repositories (`IStockMovementRepository`, `IWarehouseRequisitionRepository`), das ist das etablierte Muster:

```csharp
builder.Services.AddScoped<IProductionOrderPickingStatusRepository, ProductionOrderPickingStatusRepository>();
```

- [ ] **Step 1: Failing Tests**

```csharp
[Fact]
public async Task RunAsync_SubFaWithCoatedChild_SetsHasCoatingParts() { }

[Fact]
public async Task RunAsync_FlagFlipsToFalse_AlsoResetsIsCoatingDone() { /* Bestand: HasCoatingParts=true,
    IsCoatingDone=true; Quelle nicht mehr beschichtet -> beide false */ }

[Fact]
public async Task RunAsync_DryRun_DoesNotWriteCoatingFlag() { }
```

- [ ] **Step 2: Tests laufen lassen — muessen FAILEN**

- [ ] **Step 3: Implementieren — nach `EnsureStatusRowsAsync` (Reihenfolge ist zwingend)**

```csharp
// F5: zweite Z1-Ausnahme. Geschrieben wird ueber die etablierte Repository-Methode,
// nicht per rohem Feld-Setzen — sie kapselt den IsCoatingDone-Reset (Fallstrick #11).
// MUSS nach EnsureStatusRowsAsync laufen: SetCoatingPartsAsync aktualisiert nur
// BESTEHENDE Zeilen.
var coatingFlags = 0;
if (!dryRun)
{
    var matKeys = srcByKey.Keys.ToList();          // dieselbe Menge wie in EnsureStatusRowsAsync
    var orderIds = await _ctx.ProductionOrders
        .Where(o => o.SubOrderNumber != null && matKeys.Contains(o.SubOrderNumber))
        .Select(o => new { o.Id, o.SubOrderNumber })
        .ToListAsync(ct);

    var map = orderIds.ToDictionary(
        x => x.Id,
        x => coating.GetValueOrDefault(x.SubOrderNumber!));
    await _pickingStatus.SetCoatingPartsAsync(map);
    coatingFlags = map.Count(kv => kv.Value);
}
```

Klassenkommentar ein **zweites** Mal korrigieren: `PickingStatus` bleibt app-verwaltet, **ausser** `HasCoatingParts`.

- [ ] **Step 4: Tests gruen + Commit**

```bash
git commit -am "feat: HasCoatingParts aus der FA-Struktur ableiten und schreiben (F5)"
```

---

### Task 5: Kombigeraet-Erkennung im Struktur-Sync

**Files:**
- Modify: `IDEALAKEWMSService/Services/FaHierarchySyncService.cs`
- Test: `IDEALAKEWMSService.Tests/Services/FaHierarchySyncServiceTests.cs`

**Interfaces:**
- Produces: Counts-Key `hauptfa_mehrdeutig` im `FaHierarchy`-SyncLog

Rueckfrage 7: **einmal je Sync-Lauf**, nicht je Seitenaufruf. `FaHierarchySyncService` hat **keinen** Mail-Weg (nur `ISyncErrorNotifier` fuer zwei Fehlerfaelle) — die Meldung geht deshalb ins Aktivitaets-Protokoll, das in der Oberflaeche sichtbar ist. Das entspricht S1s eigener Begruendung („oder ganz ohne Mail, weil das Protokoll in der UI sichtbar ist").

- [ ] **Step 1: Failing Test**

```csharp
[Fact]
public async Task RunAsync_HauptFaWithTwoOrderInfoRows_LogsAmbiguityOnce()
{
    // zwei FaHierarchyOrderInfo-Zeilen zu HauptFA 1000 (verschiedene MontageAbteilung)
    // -> genau EIN Warning-Eintrag, Counts-Key hauptfa_mehrdeutig == 1
}
```

- [ ] **Step 2: Test laufen lassen — muss FAILEN**

- [ ] **Step 3: Implementieren — direkt vor `FinishSuccessAsync` (heute Z144)**

```csharp
// Kombigeraete: derselbe HauptFA mit mehreren Montage-Abteilungen. Eine Eigenschaft der
// DATEN, nicht der Anzeige — deshalb einmal je Lauf gemeldet statt je Seitenaufruf
// (Rueckfrage 7). Die Kennzeichnung in der Oberflaeche bleibt davon unberuehrt.
var ambiguousHauptFas = infos.GroupBy(i => i.HauptFA)
    .Where(g => g.Count() > 1)
    .Select(g => g.Key)
    .OrderBy(k => k)
    .ToList();
if (ambiguousHauptFas.Count > 0)
    await run.LogWarningAsync(
        $"Kombigeraete: {ambiguousHauptFas.Count} HauptFA mit mehreren Auftragskoepfen " +
        $"({string.Join(", ", ambiguousHauptFas)}). Kopfdaten werden in der FA-Liste als " +
        "mehrdeutig gekennzeichnet, es wird keine Variante ausgewaehlt.", ct: ct);
```

`FinishSuccessAsync` bekommt `["hauptfa_mehrdeutig"] = ambiguousHauptFas.Count`.

- [ ] **Step 4: Test gruen + Commit**

```bash
git commit -am "feat: Kombigeraet-Erkennung im FA-Struktur-Sync (einmal je Lauf)"
```

---

### Task 6: Mengen-Methode fuer die Auftragskoepfe

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IFaHierarchyOrderInfoRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/FaHierarchyOrderInfoRepository.cs`
- Test: `IdealAkeWms.Tests/Repositories/FaHierarchyOrderInfoRepositoryTests.cs`

**Interfaces:**
- Produces: `Task<List<FaHierarchyOrderInfo>> GetByHauptFaKeysAsync(IReadOnlyCollection<int> hauptFaKeys)` — **eine** Abfrage je Seite, kein Fan-out-Join in die Zeilenabfrage.

- [ ] **Step 1: Failing Tests**

```csharp
[Fact]
public async Task GetByHauptFaKeysAsync_ReturnsAllRowsForGivenKeys_IncludingDuplicatesPerHauptFa() { }

[Fact]
public async Task GetByHauptFaKeysAsync_EmptyInput_ReturnsEmptyWithoutQuery() { }
```

- [ ] **Step 2: Test laufen lassen — muss FAILEN**

- [ ] **Step 3: Implementieren**

```csharp
/// <summary>
/// Alle Auftragskoepfe zu einer Menge von HauptFA-Nummern — EINE Abfrage je angezeigter
/// Seite (Fan-out-Vermeidung). Liefert bei Kombigeraeten bewusst MEHRERE Zeilen je HauptFA.
/// </summary>
public async Task<List<FaHierarchyOrderInfo>> GetByHauptFaKeysAsync(IReadOnlyCollection<int> hauptFaKeys)
{
    if (hauptFaKeys == null || hauptFaKeys.Count == 0) return new List<FaHierarchyOrderInfo>();
    var keys = hauptFaKeys.Distinct().ToList();
    return await _db.FaHierarchyOrderInfos.AsNoTracking()
        .Where(o => keys.Contains(o.HauptFA))
        .OrderBy(o => o.HauptFA).ThenBy(o => o.MontageAbteilung)
        .ToListAsync();
}
```

- [ ] **Step 4: Tests gruen + Commit**

```bash
git commit -am "feat: GetByHauptFaKeysAsync fuer die gebuendelte Kopfdaten-Abfrage"
```

---

### Task 7: Kunde-Filter im hierarchischen Modus

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs`
- Test: `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs`

**Interfaces:**
- Consumes: `BuildLeitstandQuery(..., bool hierarchical)` — Parameter existiert bereits
- Wirkt auf: FA-Liste **und** Leitstand (beide nutzen dieselbe Query-Methode)

**Zwei Pfade, beide zu reparieren** (am Code bestaetigt: `filterCustomer` und der Spaltenfilter `customer` sind getrennt und UND-verknuepft):

- [ ] **Step 1: Failing Tests**

```csharp
[Fact]
public async Task Leitstand_Hierarchical_CustomerFreetextFilter_MatchesViaOrderInfo()
{
    // ProductionOrder mit Customer = null, FaHierarchyOrderInfo(HauptFA=1000, Kunde="Mustermann")
    // filterCustomer "muster" -> Zeile wird gefunden
}

[Fact]
public async Task Leitstand_Hierarchical_CustomerColumnFilter_MatchesViaOrderInfo() { }

[Fact]
public async Task Leitstand_Flat_CustomerFilter_UnchangedAgainstProductionOrderCustomer()
{
    // Master false -> nur ProductionOrder.Customer, KEIN OrderInfo-Treffer
}
```

- [ ] **Step 2: Tests laufen lassen — muessen FAILEN**

- [ ] **Step 3: Implementieren — Subquery statt Join (kein Fan-out)**

In `BuildLeitstandQuery`, Freitextpfad:

```csharp
if (!string.IsNullOrWhiteSpace(filterCustomer))
{
    if (hierarchical)
    {
        // K1: ProductionOrder.Customer ist bei IDEAL je Sub-FA leer — der Kunde haengt am
        // HauptFA. Subquery statt Join, damit die Zeilenmenge nicht aufgeblaeht wird.
        var pattern = $"%{filterCustomer}%";
        q = q.Where(o =>
            (o.Customer != null && EF.Functions.Like(o.Customer, pattern)) ||
            _context.FaHierarchyOrderInfos.Any(i =>
                i.Kunde != null && EF.Functions.Like(i.Kunde, pattern) &&
                i.HauptFA.ToString() == o.OrderNumber));
    }
    else
    {
        q = q.Where(o => o.Customer != null && EF.Functions.Like(o.Customer, $"%{filterCustomer}%"));
    }
}
```

Im `ApplyLeitstandColumnFilter`-Case `"customer"` dieselbe Erweiterung, mit der bestehenden Token-/Negations-Semantik. Bei `negate` gilt: eine Zeile faellt raus, wenn **weder** die Zeile **noch** der Kopf passt.

> **Typbruch beachten:** `FaHierarchyOrderInfo.HauptFA` ist `int`, `ProductionOrder.OrderNumber` ist `string`. Der Vergleich `i.HauptFA.ToString() == o.OrderNumber` ist das im Paket etablierte Muster (Materialisierung). Falls EF Core das nicht uebersetzen kann, stattdessen die passenden HauptFA-Schluessel in einem **eigenen** Vorab-Query ermitteln und als `IN`-Liste einsetzen — dann bitte als Ruling festhalten.

- [ ] **Step 4: Tests gruen + Commit**

```bash
git commit -am "fix: Kunde-Filter und Kunde-Spaltenfilter finden im hierarchischen Modus ueber den Auftragskopf"
```

---

### Task 8: ViewModel und Controller — K1, Termine, Beschichtungstermin

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs`
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs`
- Test: `IdealAkeWms.Tests/Controllers/ProductionOrdersControllerTests.cs`

**Interfaces:**
- Consumes: `GetByHauptFaKeysAsync` (Task 6)
- Produces: `ProductionOrderListGroup` mit `Customer`, `KonstruktionsTermin`, `ProductionDate`, `DeliveryDate`, `Prio`, `AbNummer`, `MontageAbteilung`, `IsAmbiguous`, `HeadVariants`

- [ ] **Step 1: ViewModel erweitern**

```csharp
public class ProductionOrderListGroup
{
    public string OrderNumber { get; set; } = string.Empty;
    public List<ProductionOrderListItem> Items { get; set; } = new();

    // --- K1: Auftragsdaten je HauptFA (NICHT materialisiert, nur Anzeige) ---
    public string? Customer { get; set; }
    public DateTime? KonstruktionsTermin { get; set; }   // KO_Termin
    public DateTime? ProductionDate { get; set; }        // FE_Termin
    public DateTime? DeliveryDate { get; set; }          // Verladetermin_Vsl
    public int? Prio { get; set; }
    public string? AbNummer { get; set; }
    public string? MontageAbteilung { get; set; }

    /// <summary>true = Kombigeraet (mehrere Auftragskoepfe). Dann wird KEINE Variante gewaehlt.</summary>
    public bool IsAmbiguous { get; set; }

    /// <summary>Alle Kopfvarianten im Klartext — nur bei <see cref="IsAmbiguous"/> gefuellt.</summary>
    public List<ProductionOrderHeadVariant> HeadVariants { get; set; } = new();
}

public class ProductionOrderHeadVariant
{
    public string? MontageAbteilung { get; set; }
    public string? Customer { get; set; }
    public DateTime? KonstruktionsTermin { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public int? Prio { get; set; }
    public string? AbNummer { get; set; }
}
```

- [ ] **Step 2: Failing Tests**

```csharp
[Fact]
public async Task Index_Hierarchical_FillsGroupHeadFromOrderInfo() { }

[Fact]
public async Task Index_Hierarchical_AmbiguousHauptFa_SetsFlagAndAllVariants_AndNoSingleValues()
{
    // zwei OrderInfo-Zeilen -> IsAmbiguous true, HeadVariants.Count == 2,
    // Customer/ProductionDate/... der Gruppe bleiben NULL (keine stille Auswahl)
}

[Fact]
public async Task Index_Hierarchical_UsesGroupProductionDateForCascade_NotRowValue()
{
    // ProductionOrder.ProductionDate == null, FE_Termin gesetzt
    // -> KommissionierTermin/VorkommissionierTermin berechnet, DB-Zeile bleibt null
}

[Fact]
public async Task Index_Hierarchical_TwoWorkplacesDifferentOverride_SameProductionDate_DifferentBgTermin() { }

[Fact]
public async Task Index_Hierarchical_CoatingDateFromStartBeschichtung_OnlyWhenUnambiguousAndFlagged() { }

[Fact]
public async Task Index_Hierarchical_AmbiguousHauptFa_ShowsNoCoatingDateOnAnyRow() { }
```

- [ ] **Step 3: Tests laufen lassen — muessen FAILEN**

- [ ] **Step 4: Controller implementieren**

`MapItem` bekommt zwei zusaetzliche Parameter; **die flache Aufrufstelle uebergibt `null`**, damit AKE bit-identisch bleibt:

```csharp
ProductionOrderListItem MapItem(LeitstandOrderRow o, bool hasCoatingParts, bool isCoatingDone,
    DateTime? groupProductionDate = null, DateTime? groupDeliveryDate = null,
    DateTime? groupCoatingStart = null)
```

Im Rumpf:
```csharp
// K1: Im hierarchischen Modus haengt der Fertigungstermin am HauptFA. Die DB-Zeile bleibt
// bewusst NULL — die Anreicherung passiert nur hier im Anzeige-Modell.
var effectiveProductionDate = groupProductionDate ?? o.ProductionDate;
item.ProductionDate = effectiveProductionDate;
item.DeliveryDate = groupDeliveryDate ?? o.DeliveryDate;
```
Danach die bestehende Kaskade auf `effectiveProductionDate` statt `o.ProductionDate` umstellen.

Beschichtungstermin (ersetzt fuer den hierarchischen Fall die berechnete Kaskade):
```csharp
if (groupCoatingStart.HasValue)
{
    // Bei IDEAL kommt der Beschichtungstermin explizit aus dem Auftragskopf
    // (Start_Beschichtung) — nicht aus der Rueckwaerts-Kaskade, die hier ohnehin
    // ins Leere liefe. Nur an Zeilen mit Lack-Flag.
    item.BeschichtungTermin = item.HasCoatingParts ? groupCoatingStart : null;
}
```

Nach `GetForLeitstandGroupedAsync` die Koepfe laden und zuordnen:

```csharp
var hauptFaKeys = grouped.Groups
    .Select(g => int.TryParse(g.OrderNumber, out var k) ? k : (int?)null)
    .Where(k => k.HasValue).Select(k => k!.Value).Distinct().ToList();
var headInfos = await _faHierarchyOrderInfoRepository.GetByHauptFaKeysAsync(hauptFaKeys);
var headsByFa = headInfos.GroupBy(i => i.HauptFA).ToDictionary(g => g.Key, g => g.ToList());
```

Je Gruppe: bei genau einer Kopfzeile die Einzelwerte setzen; bei mehreren `IsAmbiguous = true`, `HeadVariants` fuellen und **alle** Einzelwerte auf `null` lassen (kein stilles Auswaehlen — die Kombigeraet-Regel gilt fuer Termine, Kunde **und** den Beschichtungstermin).

- [ ] **Step 5: Tests gruen + Commit**

```bash
git commit -am "feat: K1-Auftragsdaten je HauptFA im ViewModel, Termin-Kaskade auf den Gruppenwert"
```

---

### Task 9: Gruppen-Kopfzeile in der FA-Liste

**Files:**
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml` (Gruppen-Kopfzeile, heute Z124-134)

**Gestaltung — verbindlich, aus dem `frontend-design`-Lauf.** Konsistenz vor Eigenstaendigkeit: Das Muster wird **woertlich** aus `Views/FaHierarchy/Index.cshtml:144-190` uebernommen, damit dieselben Daten in FA-Struktur und FA-Liste gleich aussehen. Kein neues Icon, keine neue Farbrolle. Die Bedeutung traegt immer Kuerzel + `title`, **nie** die Farbe allein (WCAG AA, Terminals bei schlechtem Licht).

- [ ] **Step 1: Eindeutiger Kopf — Abzeichen hinter dem Sub-FA-Zaehler**

```cshtml
@if (!group.IsAmbiguous)
{
    @if (!string.IsNullOrEmpty(group.Customer))
    {
        <span class="badge bg-primary ms-2" title="Kunde">@group.Customer</span>
    }
    @if (group.Prio.HasValue)
    {
        <span class="badge bg-secondary ms-1" title="Priorität">Prio @group.Prio</span>
    }
    @if (!string.IsNullOrEmpty(group.MontageAbteilung))
    {
        <span class="badge bg-light text-dark border ms-1" title="Montage-Abteilung">@group.MontageAbteilung</span>
    }
    @if (!string.IsNullOrEmpty(group.AbNummer))
    {
        <span class="badge bg-light text-dark border ms-1" title="AB-Nummer">AB @group.AbNummer</span>
    }
    @if (group.KonstruktionsTermin.HasValue)
    {
        <span class="badge bg-light text-dark border ms-1" title="Konstruktions-Termin">KO @group.KonstruktionsTermin.Value.ToString("dd.MM.yyyy")</span>
    }
    @if (group.ProductionDate.HasValue)
    {
        <span class="badge bg-light text-dark border ms-1" title="Fertigungstermin">FE @group.ProductionDate.Value.ToString("dd.MM.yyyy")</span>
    }
    @if (group.DeliveryDate.HasValue)
    {
        <span class="badge bg-light text-dark border ms-1" title="Liefertermin">LT @group.DeliveryDate.Value.ToString("dd.MM.yyyy")</span>
    }
}
else
{
    <span class="badge bg-info text-dark ms-2" title="HauptFA mit mehreren Auftragsköpfen (Kombigerät)">mehrdeutig (Kombigerät)</span>
}
```

- [ ] **Step 2: Mehrdeutiger Kopf — alle Varianten im Klartext**

Als **eigene** Zeile direkt unter der Kopfzeile, Muster `fa-head-table` aus der FA-Struktur:

```cshtml
@if (group.IsAmbiguous && group.HeadVariants.Any())
{
    <tr class="fa-liste-group-heads">
        <td colspan="@colCount">
            <div class="table-responsive mb-1">
                <table class="table table-sm table-bordered mb-0">
                    <thead>
                        <tr>
                            <th>Montage-Abt.</th><th>Kunde</th><th>KO-Termin</th>
                            <th>FE-Termin</th><th>Liefertermin</th><th>Prio</th><th>AB-Nr.</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var v in group.HeadVariants)
                        {
                            <tr>
                                <td>@v.MontageAbteilung</td>
                                <td>@v.Customer</td>
                                <td>@(v.KonstruktionsTermin?.ToString("dd.MM.yyyy") ?? "-")</td>
                                <td>@(v.ProductionDate?.ToString("dd.MM.yyyy") ?? "-")</td>
                                <td>@(v.DeliveryDate?.ToString("dd.MM.yyyy") ?? "-")</td>
                                <td>@(v.Prio?.ToString() ?? "-")</td>
                                <td>@v.AbNummer</td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        </td>
    </tr>
}
```

- [ ] **Step 3: Sichtpruefung**

Build + Seite im Browser oeffnen (oder Markup gegen die Tests pruefen): Kopfzeile umbricht auf schmalen Terminals sauber, die Tabelle scrollt in ihrem eigenen Container, Kontrast der Abzeichen erfuellt AA.

- [ ] **Step 4: Commit**

```bash
git commit -am "feat: Gruppen-Kopfzeile der FA-Liste zeigt Auftragsdaten, Kombigeraete alle Varianten"
```

---

### Task 10: Version, Doku, Testszenarien

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/AppVersion.cs` → `1.37.0`, Datum `2026-09-09`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` (neue Karte v1.37.0 ganz oben)
- Modify: `IdealAkeWms/Views/Help/Index.cshtml` (Kopfzeilen-Daten, Kombigeraet-Kennzeichnung, Werkbank kommt aus Sage)
- Modify: `docs/TESTSZENARIEN.md` (neues Kapitel **TS-71**)

- [ ] **Step 1: Versionen und Anwender-Changelog**

Karte im Muster der v1.36.0-Karte, Text aus Anwendersicht: Auftragsdaten in der Gruppen-Kopfzeile, Kombigeraete zeigen alle Koepfe statt einer geratenen Variante, Werkbank kommt jetzt aus Sage, Lack-Kennzeichen kommt aus der Struktur, Kunde-Suche findet wieder.

- [ ] **Step 2: TS-71 in `docs/TESTSZENARIEN.md`**

Kapitel „IDEAL — Materialisierung: Fachliche Felder (K1/K2)" mit **Vorbedingungen** (Master `true`; `Sync:ProductionOrdersEnabled = false`; die fuenf Werkbaenke angelegt; ein HauptFA mit Kombigeraet benannt) und Negativfaellen (HauptFA ohne Kopfzeile, `KO_Termin`/`FE_Termin` NULL, unbekannter Arbeitsbereich, doppelter Werkbank-Name):

- **TS-71.1** K1 in der Kopfzeile, nicht auf den Zeilen dupliziert.
- **TS-71.2** Kombigeraet: alle Varianten + Kennzeichen, keine Einzelwerte in der Kopfzeile.
- **TS-71.3** Termin-Kaskade: gleicher Fert.-Termin, unterschiedlicher BG-Termin bei abweichender Werkbank-Override; DB-Spalten bleiben leer.
- **TS-71.4** Kunde-Freitextfilter findet.
- **TS-71.5** Kunde-Spaltenfilter findet (auch im Leitstand).
- **TS-71.6** Werkbank wird gesetzt; **TS-71.7** Abweichung wird ueberschrieben und gemeldet; **TS-71.8** unbekannter Arbeitsbereich meldet ohne Zuweisung; **TS-71.9** doppelter Name weist nicht zu.
- **TS-71.10** Lack-Kennzeichen aus der Struktur; Kippen auf `false` setzt „Lackierung fertig" zurueck.
- **TS-71.11** Beschichtungstermin nur bei eindeutigem Kopf und gesetztem Lack-Flag; bei Kombigeraet **keiner**.
- **TS-71.12** **Statuszeilen (UAT-Befund):** Ein frisch materialisierter **und** ein Bestands-Sub-FA lassen sich im Leitstand freigeben; Prioritaet und Kommissionierer-Zuweisung funktionieren; Massenfreigabe zaehlt alle.
- **TS-71.13** AKE-Regression bei Master `false`: FA-Liste unveraendert, keine Kopfzeilen-Daten, Kunde-Filter wie bisher.

- [ ] **Step 3: `secondbrain/tests/testszenarien-index.md` nachziehen — ACHTUNG: im HAUPTCHECKOUT**

Nicht im Worktree. Zeile fuer Kapitel 71 im Muster der Zeile 70.

- [ ] **Step 4: Build + volle Testlaeufe**

```bash
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests
dotnet test IDEALAKEWMSService.Tests
```

- [ ] **Step 5: Commit**

```bash
git commit -am "docs: v1.37.0, Anwender-Changelog, Hilfe und TS-71"
```
