# Lagerbestellungs-Druck spiegelt GUI (Spalten + Sortierung + Filter) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Der Druck (`WarehousePicking/Print/{id}`) übernimmt die in der GUI eingestellte Spalten-Sichtbarkeit/-Reihenfolge (aus persistenten User-Preferences) sowie die aktuelle Sortierung und aktiven Spaltenfilter (live aus dem Browser).

**Architecture:** Die Details-Tabelle wird auf das volle Spalten-Preferences-Muster gehoben (view-key `WarehousePickingDetails` in `ColumnDefinitions`, `column-preferences.js` + Zahnrad). Der `Print`-Controller liest die persistierten Spalten-Prefs server-seitig, der Druck-Button hängt Sortierung+Filter als Query-Parameter an. Eine zentrale, rein funktionale Helper-Klasse (`WarehousePickingPrintLayout`) liefert pro Spalte den gerenderten Zelltext — als **eine** Quelle für Filtern, Sortieren und Drucken (DRY) — und repliziert die Vergleichslogik aus `table-filter.js`, damit der Druck dieselbe Reihenfolge erzeugt wie der Bildschirm.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10 (nur Lesen über bestehende `UserViewPreferences`), `System.Text.Json`, xUnit + FluentAssertions + Moq + EF InMemory, Vanilla-JS (`column-preferences.js`/`table-filter.js`).

**Branch / Worktree:** `feature/windows-auth-ad-users` im Worktree `.claude/worktrees/missingparts-include-pd`. Spec: [docs/superpowers/specs/2026-06-19-warehousepicking-print-spalten-sort-design.md](../specs/2026-06-19-warehousepicking-print-spalten-sort-design.md).

**WICHTIG (Standing Constraints):** KEIN Merge nach `main`, KEIN Worktree-/Branch-Cleanup ohne ausdrückliche User-Freigabe. Plan endet mit grünem Build + Tests auf dem Branch. Kein Datenmodell-/Migrations-Eingriff.

---

## File Structure

**Neu:**
- `IdealAkeWms/Services/WarehousePickingPrintLayout.cs` — reine Funktionen: Prefs parsen, sichtbare Spalten in Reihenfolge auflösen, Zelltext je Col-Key, GUI-gleiche Sortierung. + `PrintColumn`, `PrintPrefs`/`PrintPrefsColumn`.
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionPrintViewModel.cs` — Druck-ViewModel (Kopf + geordnete Spalten + Items).
- `IdealAkeWms.Tests/Services/WarehousePickingPrintLayoutTests.cs` — Unit-Tests der reinen Logik.

**Geändert:**
- `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` — neuer `WarehousePickingDetails`-ViewConfig + `GetByViewKey`-Eintrag.
- `IdealAkeWms/Controllers/WarehousePickingController.cs` — ctor + `Print`-Action.
- `IdealAkeWms/Views/WarehousePicking/Print.cshtml` — dynamische Spalten.
- `IdealAkeWms/Views/WarehousePicking/Details.cshtml` — `data-view-key`, view-/column-config JSON, `column-preferences.js`-Einbindung, Print-Button-JS.
- `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs` — Setup um Prefs-Repo erweitern + neue Tests.
- `IdealAkeWms/Views/Help/Changelog.cshtml`, `CLAUDE.md`, `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md` — Doku.

---

## Task 1: View-Key `WarehousePickingDetails` registrieren

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` (nach dem `BdeBookings`-Block, vor `GetByViewKey`; + Switch-Eintrag bei Zeile 207-217)

- [ ] **Step 1: ViewConfig hinzufügen**

In `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` direkt vor `public static ViewConfig? GetByViewKey(...)` einfügen:

```csharp
    /// <summary>
    /// WarehousePicking/Details.cshtml — Positionsliste einer Lagerbestellung.
    /// Client-Mode-Tabelle (kleine, vorgefilterte Liste je Bestellung), aber mit
    /// vollem Spalten-Preferences-Muster. pos + article-number sind Locked
    /// (Identitaet der Position). Labels muessen mit den &lt;th&gt; in Details.cshtml
    /// UND dem inline #column-config JSON uebereinstimmen.
    /// </summary>
    public static readonly ViewConfig WarehousePickingDetails = new(
        "WarehousePickingDetails", "Lagerbestellung-Positionen",
        SupportsReorder: true, SupportsSortDefault: true)
    {
        Columns =
        [
            new ColumnDef("pos",            "Pos",             Locked: true),
            new ColumnDef("article-number", "Artikel-Nr",      Locked: true),
            new ColumnDef("description",    "Bezeichnung",     Locked: false),
            new ColumnDef("requested",      "Bestellt",        Locked: false),
            new ColumnDef("picked",         "Ist",             Locked: false),
            new ColumnDef("unit",           "ME",              Locked: false),
            new ColumnDef("storage",        "Lagerplatz",      Locked: false),
            new ColumnDef("note-lager",     "Notiz Lager",     Locked: false),
            new ColumnDef("note-ek",        "Notiz EK",        Locked: false),
            new ColumnDef("shortage",       "Fehlteil-Status", Locked: false),
        ]
    };
```

- [ ] **Step 2: GetByViewKey-Eintrag**

In `GetByViewKey` (Zeile 207-217) die `_` -Zeile so erweitern:

```csharp
        "BdeBookings"      => BdeBookings,
        "WarehousePickingDetails" => WarehousePickingDetails,
        _                  => null
```

- [ ] **Step 3: Build**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
git commit -m "feat(prefs): view-key WarehousePickingDetails registrieren"
```

---

## Task 2: `WarehousePickingPrintLayout` + Print-ViewModel (TDD)

**Files:**
- Create: `IdealAkeWms/Models/ViewModels/WarehouseRequisitionPrintViewModel.cs`
- Create: `IdealAkeWms/Services/WarehousePickingPrintLayout.cs`
- Test: `IdealAkeWms.Tests/Services/WarehousePickingPrintLayoutTests.cs`

- [ ] **Step 1: Print-ViewModel + PrintColumn anlegen**

`IdealAkeWms/Models/ViewModels/WarehouseRequisitionPrintViewModel.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>Eine sichtbare Druck-Spalte in finaler Reihenfolge.</summary>
public record PrintColumn(string Key, string Label);

/// <summary>
/// Druck-ViewModel fuer WarehousePicking/Print. Items sind bereits gefiltert + sortiert,
/// Columns sind die sichtbaren Spalten in finaler Reihenfolge (aus User-Preferences).
/// </summary>
public class WarehouseRequisitionPrintViewModel
{
    public int Id { get; set; }
    public string WorkplaceName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public WarehouseRequisitionStatus Status { get; set; }
    public List<PrintColumn> Columns { get; set; } = new();
    public List<WarehouseRequisitionDetailItemViewModel> Items { get; set; } = new();
}
```

- [ ] **Step 2: Failing tests schreiben**

`IdealAkeWms.Tests/Services/WarehousePickingPrintLayoutTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class WarehousePickingPrintLayoutTests
{
    private static WarehouseRequisitionDetailItemViewModel Item(
        int pos, string art, string desc, int req, int? picked, string unit,
        string storage, string? note = null, string? noteEk = null,
        ShortageStatus shortage = ShortageStatus.None) =>
        new(pos, pos, art, desc, unit, req, picked, storage, note, shortage, noteEk);

    [Fact]
    public void ResolveColumns_NoPrefs_ReturnsAllInDefaultOrder()
    {
        var cols = WarehousePickingPrintLayout.ResolveColumns(null);

        cols.Select(c => c.Key).Should().Equal(
            "pos", "article-number", "description", "requested", "picked",
            "unit", "storage", "note-lager", "note-ek", "shortage");
    }

    [Fact]
    public void ResolveColumns_HiddenColumn_Omitted_LockedKept()
    {
        // note-ek versteckt; pos (locked) faelschlich visible=false -> bleibt trotzdem.
        var json = """
        {"columns":[
          {"key":"pos","visible":false,"order":0},
          {"key":"article-number","visible":true,"order":1},
          {"key":"description","visible":true,"order":2},
          {"key":"requested","visible":true,"order":3},
          {"key":"picked","visible":true,"order":4},
          {"key":"unit","visible":true,"order":5},
          {"key":"storage","visible":true,"order":6},
          {"key":"note-lager","visible":true,"order":7},
          {"key":"note-ek","visible":false,"order":8},
          {"key":"shortage","visible":true,"order":9}
        ],"defaultSortColumn":null,"defaultSortDirection":"asc"}
        """;

        var cols = WarehousePickingPrintLayout.ResolveColumns(json);

        cols.Select(c => c.Key).Should().Contain("pos");        // locked, trotz visible=false
        cols.Select(c => c.Key).Should().NotContain("note-ek"); // versteckt
    }

    [Fact]
    public void ResolveColumns_Reordered_RespectsOrder()
    {
        // storage (order 1) vor article-number (order 6) ziehen; pos bleibt order 0.
        var json = """
        {"columns":[
          {"key":"pos","visible":true,"order":0},
          {"key":"storage","visible":true,"order":1},
          {"key":"article-number","visible":true,"order":6},
          {"key":"description","visible":true,"order":2},
          {"key":"requested","visible":true,"order":3},
          {"key":"picked","visible":true,"order":4},
          {"key":"unit","visible":true,"order":5},
          {"key":"note-lager","visible":true,"order":7},
          {"key":"note-ek","visible":true,"order":8},
          {"key":"shortage","visible":true,"order":9}
        ],"defaultSortColumn":null,"defaultSortDirection":"asc"}
        """;

        var cols = WarehousePickingPrintLayout.ResolveColumns(json);

        cols.Select(c => c.Key).Should().Equal(
            "pos", "storage", "description", "requested", "picked", "unit",
            "article-number", "note-lager", "note-ek", "shortage");
    }

    [Fact]
    public void CellText_RendersExpectedStrings()
    {
        var i = Item(1, "ART-1", "Schraube", 5, null, "Stk", "L01",
            note: "nl", noteEk: "ek", shortage: ShortageStatus.WillBeRestocked);

        WarehousePickingPrintLayout.CellText(i, "pos").Should().Be("1");
        WarehousePickingPrintLayout.CellText(i, "article-number").Should().Be("ART-1");
        WarehousePickingPrintLayout.CellText(i, "requested").Should().Be("5");
        WarehousePickingPrintLayout.CellText(i, "picked").Should().Be("");        // null -> leer
        WarehousePickingPrintLayout.CellText(i, "shortage").Should().Be("Fehlteil");
    }

    [Fact]
    public void SortItems_ByStorageDesc_OrdersByCellText()
    {
        var items = new[]
        {
            Item(1, "A", "x", 1, null, "Stk", "L01"),
            Item(2, "B", "y", 1, null, "Stk", "L09"),
            Item(3, "C", "z", 1, null, "Stk", "L05"),
        };

        var sorted = WarehousePickingPrintLayout.SortItems(
            items, sortCol: "storage", sortDir: "desc",
            defaultSortColumn: null, defaultSortDirection: null);

        sorted.Select(s => s.StorageLocations).Should().Equal("L09", "L05", "L01");
    }

    [Fact]
    public void SortItems_ByRequested_NumericNotLexical()
    {
        var items = new[]
        {
            Item(1, "A", "x", 9,  null, "Stk", "L"),
            Item(2, "B", "y", 10, null, "Stk", "L"),
            Item(3, "C", "z", 2,  null, "Stk", "L"),
        };

        var sorted = WarehousePickingPrintLayout.SortItems(
            items, sortCol: "requested", sortDir: "asc",
            defaultSortColumn: null, defaultSortDirection: null);

        sorted.Select(s => s.QuantityRequested).Should().Equal(2m, 9m, 10m);
    }

    [Fact]
    public void SortItems_NoSortCol_UsesDefaultSortColumn()
    {
        var items = new[]
        {
            Item(1, "A", "x", 1, null, "Stk", "L02"),
            Item(2, "B", "y", 1, null, "Stk", "L01"),
        };

        var sorted = WarehousePickingPrintLayout.SortItems(
            items, sortCol: null, sortDir: null,
            defaultSortColumn: "storage", defaultSortDirection: "asc");

        sorted.Select(s => s.StorageLocations).Should().Equal("L01", "L02");
    }

    [Fact]
    public void SortItems_NoSortAtAll_KeepsPositionOrder()
    {
        var items = new[]
        {
            Item(3, "A", "x", 1, null, "Stk", "L"),
            Item(1, "B", "y", 1, null, "Stk", "L"),
            Item(2, "C", "z", 1, null, "Stk", "L"),
        };

        var sorted = WarehousePickingPrintLayout.SortItems(
            items, sortCol: null, sortDir: null,
            defaultSortColumn: null, defaultSortDirection: null);

        sorted.Select(s => s.Position).Should().Equal(1, 2, 3); // Fallback = pos asc
    }

    [Fact]
    public void ParsePrefs_InvalidJson_ReturnsNull()
    {
        WarehousePickingPrintLayout.ParsePrefs("not-json").Should().BeNull();
        WarehousePickingPrintLayout.ParsePrefs(null).Should().BeNull();
        WarehousePickingPrintLayout.ParsePrefs("").Should().BeNull();
    }
}
```

- [ ] **Step 3: Run tests to verify they fail to compile**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehousePickingPrintLayoutTests"`
Expected: FAIL — `WarehousePickingPrintLayout` existiert nicht.

- [ ] **Step 4: Helper implementieren**

`IdealAkeWms/Services/WarehousePickingPrintLayout.cs`:

```csharp
using System.Globalization;
using System.Text.Json;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>
/// Reine Druck-Layout-Logik fuer WarehousePicking/Print: Spalten-Reihenfolge/-Sichtbarkeit
/// aus User-Preferences, gerenderter Zelltext je Col-Key (EINE Quelle fuer Filter/Sort/Druck),
/// und GUI-gleiche Sortierung (repliziert table-filter.js sortTable).
/// </summary>
public static class WarehousePickingPrintLayout
{
    /// <summary>Deserialisierungs-Ziel fuer das in UserViewPreferences.SettingsJson gespeicherte Format.</summary>
    public sealed class PrintPrefs
    {
        public List<PrintPrefsColumn>? Columns { get; set; }
        public string? DefaultSortColumn { get; set; }
        public string? DefaultSortDirection { get; set; }
    }

    public sealed class PrintPrefsColumn
    {
        public string Key { get; set; } = string.Empty;
        public bool Visible { get; set; } = true;
        public int Order { get; set; }
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static PrintPrefs? ParsePrefs(string? settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson)) return null;
        try { return JsonSerializer.Deserialize<PrintPrefs>(settingsJson, JsonOpts); }
        catch (JsonException) { return null; }
    }

    /// <summary>Sichtbare Spalten in finaler Reihenfolge. Locked-Spalten bleiben immer sichtbar.</summary>
    public static List<PrintColumn> ResolveColumns(string? settingsJson)
        => ResolveColumns(ParsePrefs(settingsJson));

    public static List<PrintColumn> ResolveColumns(PrintPrefs? prefs)
    {
        var defs = ColumnDefinitions.WarehousePickingDetails.Columns;
        if (prefs?.Columns == null || prefs.Columns.Count == 0)
            return defs.Select(d => new PrintColumn(d.Key, d.Label)).ToList();

        var byKey = new Dictionary<string, PrintPrefsColumn>();
        foreach (var c in prefs.Columns)
            if (!string.IsNullOrEmpty(c.Key)) byKey[c.Key] = c; // letzter gewinnt -> dup-sicher

        return defs
            .Select((d, idx) =>
            {
                byKey.TryGetValue(d.Key, out var p);
                var visible = d.Locked || (p?.Visible ?? true);
                var order = p?.Order ?? idx;
                return (def: d, visible, order);
            })
            .Where(x => x.visible)
            .OrderBy(x => x.order)
            .Select(x => new PrintColumn(x.def.Key, x.def.Label))
            .ToList();
    }

    /// <summary>Gerenderter Zelltext je Col-Key — identisch fuer Filter, Sort und Druck.</summary>
    public static string CellText(WarehouseRequisitionDetailItemViewModel i, string key) => key switch
    {
        "pos"            => i.Position.ToString(CultureInfo.InvariantCulture),
        "article-number" => i.ArticleNumber ?? string.Empty,
        "description"    => i.ArticleDescription ?? string.Empty,
        "requested"      => Round(i.QuantityRequested).ToString(CultureInfo.InvariantCulture),
        "picked"         => i.QuantityPicked.HasValue
                                ? Round(i.QuantityPicked.Value).ToString(CultureInfo.InvariantCulture)
                                : string.Empty,
        "unit"           => i.Unit ?? string.Empty,
        "storage"        => i.StorageLocations ?? string.Empty,
        "note-lager"     => i.Note ?? string.Empty,
        "note-ek"        => i.NoteEinkauf ?? string.Empty,
        "shortage"       => i.ShortageStatus switch
        {
            ShortageStatus.WillBeRestocked => "Fehlteil",
            ShortageStatus.NoRestock       => "Wird nicht nachgeliefert",
            _                              => string.Empty
        },
        _                => string.Empty
    };

    private static int Round(decimal d) => (int)Math.Round(d, MidpointRounding.AwayFromZero);

    /// <summary>Col-Key -> Zelltext, fuer ColumnFilterHelper.Apply.</summary>
    public static readonly IReadOnlyDictionary<string, Func<WarehouseRequisitionDetailItemViewModel, string?>> ColumnMap =
        ColumnDefinitions.WarehousePickingDetails.Columns
            .ToDictionary(d => d.Key, d => (Func<WarehouseRequisitionDetailItemViewModel, string?>)(i => CellText(i, d.Key)));

    /// <summary>
    /// Stabile Sortierung wie der Bildschirm: effektive Spalte = sortCol ?? defaultSortColumn ?? "pos".
    /// Vergleich repliziert table-filter.js (numerisch wenn beide Zahlen, sonst de-Culture-String).
    /// </summary>
    public static List<WarehouseRequisitionDetailItemViewModel> SortItems(
        IEnumerable<WarehouseRequisitionDetailItemViewModel> items,
        string? sortCol, string? sortDir, string? defaultSortColumn, string? defaultSortDirection)
    {
        var list = items.ToList();

        string col;
        string dir;
        if (!string.IsNullOrWhiteSpace(sortCol))
        {
            col = sortCol!;
            dir = string.IsNullOrWhiteSpace(sortDir) ? "asc" : sortDir!;
        }
        else if (!string.IsNullOrWhiteSpace(defaultSortColumn))
        {
            col = defaultSortColumn!;
            dir = string.IsNullOrWhiteSpace(defaultSortDirection) ? "asc" : defaultSortDirection!;
        }
        else
        {
            col = "pos";
            dir = "asc";
        }

        if (!ColumnMap.ContainsKey(col)) { col = "pos"; dir = "asc"; }

        var cmp = Comparer<WarehouseRequisitionDetailItemViewModel>.Create((a, b) =>
            CompareLikeGui(CellText(a, col), CellText(b, col)));

        // OrderBy ist stabil -> gleiche Werte behalten Position-Reihenfolge (Eingangsreihenfolge).
        return dir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? list.OrderByDescending(x => x, cmp).ToList()
            : list.OrderBy(x => x, cmp).ToList();
    }

    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Numerisch wenn beide als Zahl (de-Format: Tausender '.', Dezimal ',') parsen, sonst de-String.</summary>
    internal static int CompareLikeGui(string a, string b)
    {
        if (TryNum(a, out var na) && TryNum(b, out var nb))
            return na.CompareTo(nb);
        return string.Compare(a, b, De, CompareOptions.None);
    }

    private static bool TryNum(string s, out double value)
    {
        var cleaned = (s ?? string.Empty).Replace(".", string.Empty).Replace(',', '.');
        return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehousePickingPrintLayoutTests"`
Expected: PASS (alle 9 Tests grün).

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Services/WarehousePickingPrintLayout.cs IdealAkeWms/Models/ViewModels/WarehouseRequisitionPrintViewModel.cs IdealAkeWms.Tests/Services/WarehousePickingPrintLayoutTests.cs
git commit -m "feat(print): WarehousePickingPrintLayout (Spalten/Sort/Zelltext, GUI-gleich)"
```

---

## Task 3: `Print`-Controller auf Prefs + Sort + Filter umstellen (TDD)

**Files:**
- Modify: `IdealAkeWms/Controllers/WarehousePickingController.cs` (ctor Zeile 18-28; `Print` Zeile 313-338)
- Modify: `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs` (Setup Zeile 19-41 + neue Tests)

- [ ] **Step 1: Test-Setup um Prefs-Repo erweitern + neue Tests schreiben**

In `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs` die `Setup`-Methode so ändern, dass sie einen gemockten `IUserViewPreferenceRepository` durchreicht und zurückgibt. Ersetze die `Setup`-Signatur + ctor-Zeile:

```csharp
    private static (WarehousePickingController ctrl, ApplicationDbContext ctx, int userId,
        Mock<IUserViewPreferenceRepository> viewPrefs) Setup()
    {
        var ctx = TestDbContextFactory.Create();
        var u = new User { Name = "stocker", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.Users.Add(u); ctx.SaveChanges();

        var current = new Mock<ICurrentUserService>();
        current.Setup(s => s.GetCurrentAppUserId()).Returns(u.Id);
        current.Setup(s => s.GetDisplayName()).Returns("stocker");
        current.Setup(s => s.GetWindowsUserName()).Returns("DOMAIN\\stocker");

        var repo = new WarehouseRequisitionRepository(ctx);
        var workplaces = new ProductionWorkplaceRepository(ctx);
        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.GetCurrentStockAsync(It.IsAny<string>(), null, null, null))
             .ReturnsAsync(new List<StockOverviewItem>());

        var viewPrefs = new Mock<IUserViewPreferenceRepository>();
        viewPrefs.Setup(p => p.GetByUserAndViewAsync(It.IsAny<int>(), It.IsAny<string>()))
                 .ReturnsAsync((UserViewPreference?)null); // Default: keine Prefs

        var ctrl = new WarehousePickingController(repo, workplaces, stock.Object, current.Object, viewPrefs.Object);
        ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        return (ctrl, ctx, u.Id, viewPrefs);
    }
```

> **Hinweis:** Alle bestehenden `Setup()`-Aufrufe in dieser Datei dekonstruieren `(ctrl, ctx, userId)`. Diese müssen auf das 4-Tupel angepasst werden — wo `viewPrefs` nicht gebraucht wird: `var (ctrl, ctx, userId, _) = Setup();`. Passe ALLE bestehenden Aufrufstellen entsprechend an (sonst Compile-Fehler).

Neue Helfer + Tests am Ende der Klasse (vor schließender `}`) hinzufügen:

```csharp
    private static WarehouseRequisition SeedOrderWithItems(ApplicationDbContext ctx)
    {
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp); ctx.SaveChanges();

        var r = new WarehouseRequisition
        {
            ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.Submitted,
            SubmittedAt = DateTime.Now, CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x",
            Items = new List<WarehouseRequisitionItem>
            {
                new() { Position = 1, ArticleNumber = "ART-A", ArticleDescription = "Alpha", Unit = "Stk", QuantityRequested = 1, CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" },
                new() { Position = 2, ArticleNumber = "ART-B", ArticleDescription = "Beta",  Unit = "Stk", QuantityRequested = 1, CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" },
                new() { Position = 3, ArticleNumber = "ART-C", ArticleDescription = "Gamma", Unit = "Stk", QuantityRequested = 1, CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" },
            }
        };
        ctx.WarehouseRequisitions.Add(r); ctx.SaveChanges();
        return r;
    }

    [Fact]
    public async Task Print_NoPrefs_AllColumns_PositionOrder()
    {
        var (ctrl, ctx, _, _) = Setup();
        var r = SeedOrderWithItems(ctx);

        var result = await ctrl.Print(r.Id, sortCol: null, sortDir: null) as ViewResult;
        var vm = result!.Model as WarehouseRequisitionPrintViewModel;

        vm!.Columns.Select(c => c.Key).Should().Equal(
            "pos", "article-number", "description", "requested", "picked",
            "unit", "storage", "note-lager", "note-ek", "shortage");
        vm.Items.Select(i => i.Position).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Print_SortParam_SortsItems()
    {
        var (ctrl, ctx, _, _) = Setup();
        var r = SeedOrderWithItems(ctx);

        var result = await ctrl.Print(r.Id, sortCol: "article-number", sortDir: "desc") as ViewResult;
        var vm = result!.Model as WarehouseRequisitionPrintViewModel;

        vm!.Items.Select(i => i.ArticleNumber).Should().Equal("ART-C", "ART-B", "ART-A");
    }

    [Fact]
    public async Task Print_WithPrefs_AppliesVisibilityAndOrder()
    {
        var (ctrl, ctx, userId, viewPrefs) = Setup();
        var r = SeedOrderWithItems(ctx);

        var json = """
        {"columns":[
          {"key":"pos","visible":true,"order":0},
          {"key":"article-number","visible":true,"order":1},
          {"key":"description","visible":true,"order":2},
          {"key":"requested","visible":true,"order":3},
          {"key":"picked","visible":true,"order":4},
          {"key":"unit","visible":true,"order":5},
          {"key":"storage","visible":true,"order":6},
          {"key":"note-lager","visible":true,"order":7},
          {"key":"note-ek","visible":false,"order":8},
          {"key":"shortage","visible":true,"order":9}
        ],"defaultSortColumn":null,"defaultSortDirection":"asc"}
        """;
        viewPrefs.Setup(p => p.GetByUserAndViewAsync(userId, "WarehousePickingDetails"))
                 .ReturnsAsync(new UserViewPreference { UserId = userId, ViewKey = "WarehousePickingDetails", SettingsJson = json });

        var result = await ctrl.Print(r.Id, sortCol: null, sortDir: null) as ViewResult;
        var vm = result!.Model as WarehouseRequisitionPrintViewModel;

        vm!.Columns.Select(c => c.Key).Should().NotContain("note-ek");
        vm.Columns.Select(c => c.Key).Should().Contain("pos");
    }
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehousePickingControllerTests"`
Expected: FAIL — ctor-Signatur (5 Args) + `Print(id, sortCol, sortDir)` + `WarehouseRequisitionPrintViewModel` existieren noch nicht.

- [ ] **Step 3: Controller — ctor erweitern**

In `IdealAkeWms/Controllers/WarehousePickingController.cs` das Feld + ctor (Zeile 18-28) ersetzen:

```csharp
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly IStockMovementRepository _stock;
    private readonly ICurrentUserService _user;
    private readonly IUserViewPreferenceRepository _viewPrefs;

    public WarehousePickingController(
        IWarehouseRequisitionRepository repo,
        IProductionWorkplaceRepository workplaces,
        IStockMovementRepository stock,
        ICurrentUserService user,
        IUserViewPreferenceRepository viewPrefs)
    {
        _repo = repo;
        _workplaces = workplaces;
        _stock = stock;
        _user = user;
        _viewPrefs = viewPrefs;
    }
```

> Falls oben bereits `_repo`/`_workplaces`/`_stock`/`_user` deklariert sind: nur `_viewPrefs`-Feld + die zwei ctor-Zeilen ergänzen, Felder nicht doppelt deklarieren.

- [ ] **Step 4: Controller — `Print`-Action ersetzen**

Ersetze die komplette `Print`-Action (Zeile 313-338) durch:

```csharp
    public async Task<IActionResult> Print(int id, string? sortCol = null, string? sortDir = null)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null || r.Status == WarehouseRequisitionStatus.Draft) return NotFound();

        var detailItems = new List<WarehouseRequisitionDetailItemViewModel>();
        foreach (var i in r.Items.OrderBy(x => x.Position))
        {
            var stock = await _stock.GetCurrentStockAsync(filterArticle: i.ArticleNumber);
            var locationStr = string.Join(", ", stock.Where(s => s.CurrentQuantity > 0)
                .Select(s => $"{s.StorageLocationCode} ({s.CurrentQuantity:N3})"));
            detailItems.Add(new WarehouseRequisitionDetailItemViewModel(
                i.Id, i.Position, i.ArticleNumber, i.ArticleDescription, i.Unit,
                i.QuantityRequested, i.QuantityPicked, locationStr, i.Note, i.ShortageStatus, i.NoteEinkauf));
        }

        // Spalten-Preferences (Sichtbarkeit/Reihenfolge + konfigurierter Default-Sort) des Users lesen.
        WarehousePickingPrintLayout.PrintPrefs? prefs = null;
        var userId = _user.GetCurrentAppUserId();
        if (userId.HasValue)
        {
            var pref = await _viewPrefs.GetByUserAndViewAsync(userId.Value, "WarehousePickingDetails");
            prefs = WarehousePickingPrintLayout.ParsePrefs(pref?.SettingsJson);
        }

        // Filter (live aus ?colf_*) -> Sortierung (live ?sortCol/sortDir, sonst Default-Sort) -> sichtbare Spalten.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(detailItems, columnFilters, WarehousePickingPrintLayout.ColumnMap);
        var sorted = WarehousePickingPrintLayout.SortItems(
            filtered, sortCol, sortDir, prefs?.DefaultSortColumn, prefs?.DefaultSortDirection);
        var columns = WarehousePickingPrintLayout.ResolveColumns(prefs);

        var vm = new WarehouseRequisitionPrintViewModel
        {
            Id = r.Id,
            WorkplaceName = r.ProductionWorkplace?.Name ?? "",
            CreatedBy = r.CreatedBy,
            SubmittedAt = r.SubmittedAt,
            Status = r.Status,
            Columns = columns,
            Items = sorted
        };
        return View(vm);
    }
```

> `ColumnFilterHelper` + `WarehousePickingPrintLayout` liegen in `IdealAkeWms.Services` — falls nicht schon `using IdealAkeWms.Services;` oben in der Datei steht, ergänzen. `WarehouseRequisitionPrintViewModel`/`ColumnDefinitions` liegen in `IdealAkeWms.Models.ViewModels` (vermutlich bereits importiert).

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehousePickingControllerTests"`
Expected: PASS (bestehende + 3 neue Print-Tests grün).

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Controllers/WarehousePickingController.cs IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs
git commit -m "feat(print): Print liest Spalten-Prefs + Live-Sort/-Filter"
```

---

## Task 4: `Print.cshtml` auf dynamische Spalten umstellen

**Files:**
- Modify: `IdealAkeWms/Views/WarehousePicking/Print.cshtml` (komplett ersetzen)

- [ ] **Step 1: Print.cshtml ersetzen**

```razor
@model IdealAkeWms.Models.ViewModels.WarehouseRequisitionPrintViewModel
@using IdealAkeWms.Models
@using IdealAkeWms.Services
@{
    Layout = null;
}
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="utf-8" />
    <title>Lagerbestellung #@Model.Id</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Arial, sans-serif; color: #000; background: #fff; padding: 20px; font-size: 11px; }
        .screen-controls { padding: 15px; text-align: center; background: #053153; color: #fff; margin: -20px -20px 20px -20px; }
        .screen-controls button { background: #fff; color: #053153; border: none; padding: 8px 24px; font-size: 16px; cursor: pointer; border-radius: 4px; margin: 0 5px; }
        .header-info { margin-bottom: 15px; border-bottom: 2px solid #053153; padding-bottom: 10px; }
        .header-info h1 { font-size: 18px; color: #053153; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        th, td { border: 1px solid #888; padding: 4px 6px; text-align: left; }
        th { background: #f0f0f0; }
        @@media print { .screen-controls { display: none; } body { padding: 0; } }
    </style>
</head>
<body>
    <div class="screen-controls">
        <button onclick="window.print()">Drucken</button>
        <button onclick="window.close()">Schliessen</button>
    </div>
    <div class="header-info">
        <h1>Lagerbestellung #@Model.Id</h1>
        <div><strong>Werkbank:</strong> @Model.WorkplaceName &nbsp; <strong>Erfasser:</strong> @Model.CreatedBy</div>
        <div><strong>Submit:</strong> @(Model.SubmittedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—")</div>
        <div><strong>Status:</strong>
            @switch (Model.Status)
            {
                case IdealAkeWms.Models.WarehouseRequisitionStatus.PartiallyDelivered: <span>Teilgeliefert</span> break;
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Closed: <span>Abgeschlossen</span> break;
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted: <span>Eingereicht (Druck-Zwischenstand)</span> break;
                default: <span>—</span> break;
            }
        </div>
    </div>
    <table>
        <thead>
            <tr>
                @foreach (var col in Model.Columns)
                {
                    <th>@col.Label</th>
                }
            </tr>
        </thead>
        <tbody>
        @foreach (var i in Model.Items)
        {
            <tr>
                @foreach (var col in Model.Columns)
                {
                    <td>@WarehousePickingPrintLayout.CellText(i, col.Key)</td>
                }
            </tr>
        }
        </tbody>
    </table>
</body>
</html>
```

- [ ] **Step 2: Build to verify Razor compiles**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/Views/WarehousePicking/Print.cshtml
git commit -m "feat(print): Print.cshtml rendert Spalten dynamisch"
```

---

## Task 5: `Details.cshtml` — Spalten-Preferences + Print-Button-Parameter

**Files:**
- Modify: `IdealAkeWms/Views/WarehousePicking/Details.cshtml` (Tabellen-Tag Zeile 51; `@section Scripts` Zeile 195-387)

- [ ] **Step 1: Tabellen-Tag mit data-view-key**

In `IdealAkeWms/Views/WarehousePicking/Details.cshtml` die Tabellen-Zeile

```html
    <table class="table table-sm filterable-table">
```

ersetzen durch:

```html
    <table class="table table-sm filterable-table" data-view-key="WarehousePickingDetails" data-fallback-sort-column="pos" data-fallback-sort-direction="asc">
```

- [ ] **Step 2: view-config + column-config JSON-Blöcke einfügen**

Direkt **vor** dem `@section Scripts {`-Block (ungefähr Zeile 194, nach dem schließenden `</form>`/Content) einfügen:

```html
<script type="application/json" id="view-config">
{ "viewKey": "WarehousePickingDetails", "supportsReorder": true, "supportsSortDefault": true }
</script>
<script type="application/json" id="column-config">
[
    { "key": "pos", "label": "Pos", "locked": true, "defaultWidth": null },
    { "key": "article-number", "label": "Artikel-Nr", "locked": true, "defaultWidth": null },
    { "key": "description", "label": "Bezeichnung", "locked": false, "defaultWidth": null },
    { "key": "requested", "label": "Bestellt", "locked": false, "defaultWidth": null },
    { "key": "picked", "label": "Ist", "locked": false, "defaultWidth": null },
    { "key": "unit", "label": "ME", "locked": false, "defaultWidth": null },
    { "key": "storage", "label": "Lagerplatz", "locked": false, "defaultWidth": null },
    { "key": "note-lager", "label": "Notiz Lager", "locked": false, "defaultWidth": null },
    { "key": "note-ek", "label": "Notiz EK", "locked": false, "defaultWidth": null },
    { "key": "shortage", "label": "Fehlteil-Status", "locked": false, "defaultWidth": null }
]
</script>
```

- [ ] **Step 3: column-preferences.js VOR table-filter.js einbinden**

Im `@section Scripts {` die erste Zeile

```html
<script src="~/js/table-filter.js" asp-append-version="true"></script>
```

ersetzen durch (Reihenfolge ist Pflicht — `column-preferences.js` dispatcht `column-preferences-ready`):

```html
<script src="~/js/column-preferences.js" asp-append-version="true"></script>
<script src="~/js/table-filter.js" asp-append-version="true"></script>
```

- [ ] **Step 4: Print-Button-Handler — Sort/Filter als Query anhängen**

Im Inline-`<script>` den bestehenden Print-Button-Block

```javascript
    if (printBtn) {
        printBtn.addEventListener('click', async (e) => {
            if (!dirty) return;
            e.preventDefault();
            const href = printBtn.getAttribute('href');
            const win = window.open('about:blank', '_blank');
            await saveProgress();
            if (win) win.location = href; else window.location = href;
        });
    }
```

ersetzen durch:

```javascript
    function buildPrintUrl() {
        const base = printBtn.getAttribute('href');
        const params = new URLSearchParams();
        // Live-Sortierung: aktuell sortierter Spaltenkopf
        const sortedTh = document.querySelector('table.filterable-table th[data-sort-dir]');
        if (sortedTh) {
            const k = sortedTh.getAttribute('data-col-key');
            const d = sortedTh.getAttribute('data-sort-dir');
            if (k && d) { params.set('sortCol', k); params.set('sortDir', d); }
        }
        // Live-Filter: window.getActiveFilters() aus table-filter.js ({ colKey: value })
        if (typeof window.getActiveFilters === 'function') {
            const filters = window.getActiveFilters();
            Object.keys(filters).forEach(k => params.set('colf_' + k, filters[k]));
        }
        const qs = params.toString();
        return qs ? (base + (base.indexOf('?') >= 0 ? '&' : '?') + qs) : base;
    }

    if (printBtn) {
        printBtn.addEventListener('click', async (e) => {
            e.preventDefault();
            const url = buildPrintUrl();
            if (dirty) {
                const win = window.open('about:blank', '_blank');
                await saveProgress();
                if (win) win.location = url; else window.location = url;
            } else {
                window.open(url, '_blank');
            }
        });
    }
```

- [ ] **Step 5: Build to verify Razor compiles**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

- [ ] **Step 6: Manuelle Verifikation (lokal, falls möglich) — kurz**

App starten, `WarehousePicking/Details/<id>` öffnen:
- Zahnrad erscheint (von `column-preferences.js` automatisch eingefügt).
- Spalte ausblenden/umordnen → bleibt nach Reload erhalten.
- Klick-Sort + Filter funktionieren weiterhin (table-filter.js).
- „Drucken" → die geöffnete Print-URL enthält `?sortCol=…&sortDir=…` bzw. `&colf_…` und die Druckseite spiegelt Spalten/Sort/Filter.
- SaveProgress (Mengen/Notizen) funktioniert weiterhin (keine Regression durch Spalten-Reorder).

> Falls keine lokale Instanz: durch TESTSZENARIEN Kapitel 42 abgedeckt (Task 6).

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Views/WarehousePicking/Details.cshtml
git commit -m "feat(print): Details-Tabelle Spalten-Prefs + Print-Button uebergibt Sort/Filter"
```

---

## Task 6: Doku + Changelog

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` (v1.23.0-Karte `<ul>`)
- Modify: `CLAUDE.md` (Pagination/Filter-Abschnitt + Fallstrick)
- Modify: `docs/TESTSZENARIEN.md` (neues Kapitel 42, vor dem `*Ende des Dokuments*`-Footer)
- Modify: `PROJECT_STATUS.md` (v1.23.0-Abschnitt)

- [ ] **Step 1: Changelog-Bullet**

In `IdealAkeWms/Views/Help/Changelog.cshtml` in der **v1.23.0**-Karte am Ende der `<ul>` (vor `</ul>`) ergänzen:

```razor
                    <li><strong>Lagerbestellungs-Druck spiegelt die Ansicht:</strong> Der Druck einer
                        Lagerbestellung übernimmt jetzt die in der Detailansicht eingestellten
                        Spalten (Ein-/Ausblenden + Reihenfolge über das neue Zahnrad-Menü), die
                        aktuelle Sortierung und die aktiven Spaltenfilter — gedruckt wird, was man
                        sieht.</li>
```

- [ ] **Step 2: CLAUDE.md — server-filter/prefs-Liste + Fallstrick**

(a) Im Abschnitt „Pagination & Server-Side Spaltenfilter" in der Liste „Aktuelle Liste der server-filter-Tabellen" am Ende den Hinweis ergänzen (Client-Mode-Sonderfall):

```
**Client-Mode mit Spalten-Prefs (Sonderfall):** WarehousePicking/Details (Lagerbestellung-Positionen) — `filterable-table` (Client-Sort/-Filter) PLUS volles Spalten-Preferences-Muster (view-key `WarehousePickingDetails`, Zahnrad). Der Druck (`Print`) spiegelt Spalten/Sort/Filter (Spalten aus Prefs server-seitig, Sort+Filter live als `?sortCol/sortDir`/`?colf_*`).
```

(b) Neuer Fallstrick im Abschnitt „Bekannte Fallstricke":

```
- **Lagerbestellungs-Druck = GUI-Spiegelung (seit v1.23.0)**: `WarehousePicking/Print` rendert Spalten **dynamisch** aus den persistierten Spalten-Preferences (view-key `WarehousePickingDetails`, server-seitig via `IUserViewPreferenceRepository`) und wendet Live-Sort (`?sortCol`/`?sortDir`) + Live-Filter (`?colf_*`) an. EINE Quelle für Filter/Sort/Druck-Zelltext: `WarehousePickingPrintLayout.CellText` — die Sortierung repliziert bewusst die Vergleichslogik aus `table-filter.js` (numerisch wenn beide Werte als de-Zahl parsen, sonst de-Culture-String), damit der Druck dieselbe Reihenfolge liefert wie der Bildschirm. `pos` + `article-number` sind Locked (immer im Druck). Bei neuen Spalten: `ColumnDefinitions.WarehousePickingDetails` UND das inline `#column-config` in Details.cshtml UND `WarehousePickingPrintLayout.CellText` synchron halten.
```

- [ ] **Step 3: TESTSZENARIEN Kapitel 42**

In `docs/TESTSZENARIEN.md` direkt vor der Zeile `*Ende des Dokuments. Stand: v1.23.0 (2026-06-18)*` (und der zugehörigen `---` darüber) einfügen:

```markdown
## Kapitel 42: Lagerbestellungs-Druck spiegelt GUI (Spalten/Sort/Filter) (v1.23.0)

**Vorbedingung:** `BestellungenAktiv=true`. Eine Lagerbestellung mit mehreren Positionen
(Status Abgeschickt/Teilgeliefert). Angemeldet als Lager-/Picking-/Admin-User.

### TS-42.1 Spalten ein-/ausblenden + Reihenfolge
1. `WarehousePicking/Details/<id>` öffnen. Zahnrad-Menü (rechts in der Tabelle) erscheint.
2. Spalte „Notiz EK" ausblenden, „Lagerplatz" nach vorne ziehen. Seite neu laden.
   - **Erwartet:** Einstellung bleibt erhalten (persistent pro User).
3. „Drucken" klicken.
   - **Erwartet:** Druckseite zeigt „Notiz EK" NICHT, „Lagerplatz" an der verschobenen Position;
     `pos` + „Artikel-Nr" sind immer vorhanden (nicht ausblendbar).

### TS-42.2 Sortierung übernehmen
1. In Details auf den Spaltenkopf „Lagerplatz" klicken (absteigend sortieren).
2. „Drucken".
   - **Erwartet:** Druck-Zeilen in derselben Reihenfolge wie der Bildschirm (Lagerplatz absteigend).
3. Numerische Spalte testen: nach „Bestellt" sortieren (z. B. 2, 9, 10).
   - **Erwartet:** numerische Reihenfolge (2, 9, 10) — NICHT lexikalisch (10, 2, 9).

### TS-42.3 Filter übernehmen
1. In Details einen Spaltenfilter setzen (z. B. „Artikel-Nr" enthält einen Teilstring).
2. „Drucken".
   - **Erwartet:** Druck zeigt nur die gefilterten (sichtbaren) Zeilen.

### TS-42.4 Default-Zustand (Regression)
1. Neuer User ohne Spalten-Einstellungen, kein Klick-Sort, kein Filter → „Drucken".
   - **Erwartet:** alle 10 Spalten in Standard-Reihenfolge, sortiert nach Position — wie bisher.

### TS-42.5 Mengen/Notizen-Speichern (Regression)
1. In Details Menge/Notiz ändern, dann „Drucken".
   - **Erwartet:** Änderung wird vor dem Druck gespeichert (Autosave), Druck zeigt den neuen Stand;
     Spalten-Reorder hat das Speichern nicht beschädigt.
```

- [ ] **Step 4: PROJECT_STATUS.md**

In `PROJECT_STATUS.md` im v1.23.0-Abschnitt einen Bullet ergänzen (z. B. nach dem `lagerbestellung`-Eintrag):

```markdown
- Lagerbestellungs-Druck (`WarehousePicking/Print`) spiegelt GUI: Spalten-Sichtbarkeit/-Reihenfolge
  (neues Zahnrad-Prefs-Muster, view-key `WarehousePickingDetails`) + Live-Sortierung + Live-Filter.
  Reine Layout-Logik in `WarehousePickingPrintLayout` (GUI-gleicher Vergleich). Kein Migrations-Eingriff.
```

- [ ] **Step 5: Build + Commit**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

```bash
git add IdealAkeWms/Views/Help/Changelog.cshtml CLAUDE.md docs/TESTSZENARIEN.md PROJECT_STATUS.md
git commit -m "docs(v1.23.0): Lagerbestellungs-Druck spiegelt GUI-Spalten/Sort/Filter"
```

---

## Task 7: Final-Check

**Files:** keine.

- [ ] **Step 1: Voller Build**

Run: `dotnet build IdealAkeWms.Tests/IdealAkeWms.Tests.csproj`
Expected: Build succeeded, 0 Errors (kompiliert das Web-Projekt mit).

- [ ] **Step 2: Volle Web-Testsuite**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj`
Expected: Alle grün (Baseline 742 + 9 Helper + 3 Controller = ~754; tatsächliche Zahl prüfen, keine Regression, 1 Skip wie gehabt).

- [ ] **Step 3: Sanity — keine versehentliche Migration**

Run: `dotnet ef migrations has-pending-model-changes --project IdealAkeWms/IdealAkeWms.csproj`
Expected: „No changes" (Feature ist reines Lese-/View-Feature, kein Modell-Change).

> **STOP — keine Merge-/Cleanup-Aktion.** Plan endet hier. Merge nach `main` + Worktree-/Branch-Cleanup nur auf ausdrückliche User-Freigabe.

---

## Self-Review (vom Plan-Autor)

**Spec-Coverage:**
- Spec §3.1/§3.4 Spalten-Prefs aktivieren (view-key, Zahnrad, JS-Reihenfolge) → Task 1 + Task 5. ✅
- Spec §3.2/§3.3 Hybrid (Spalten server-read, Sort+Filter live) → Task 3 (Controller) + Task 5 (Print-Button-Params). ✅
- Spec §3.5 Col-Key→Render/Sort/Filter Single-Source → `WarehousePickingPrintLayout.CellText` + `ColumnMap`, Task 2. ✅
- Spec §4 Edge Cases (keine Prefs, kein Sort, ungültiger Key, 0 Zeilen, Locked) → Task 2 Tests + ResolveColumns/SortItems-Logik. ✅
- Spec §5 Tests → Task 2 (Helper-Units) + Task 3 (Controller) + Task 6 (TESTSZENARIEN). ✅
- Spec §6 Doku/Version → Task 6. ✅
- Spec §7 Risiko column-preferences im Client-Mode + JSON-Format → Task 5 Step 6 (manuelle Verifikation) + Task 2 (ParsePrefs mit camelCase, dup-sicher). ✅

**Placeholder-Scan:** Keine TBD/TODO. Alle Code-Schritte enthalten vollständigen Code. Einzige „nach Bedarf"-Stelle (Task 3 Step 3: Felder nicht doppelt deklarieren / using ergänzen) ist eine konkrete, verifizierbare Anweisung, kein Platzhalter.

**Typ-Konsistenz:** `WarehousePickingPrintLayout.ResolveColumns(string?)` + `(PrintPrefs?)`-Overloads, `ParsePrefs`, `CellText`, `ColumnMap`, `SortItems(items, sortCol, sortDir, defaultSortColumn, defaultSortDirection)` — identisch in Tests (Task 2/3), Controller (Task 3) und View (Task 4). `WarehouseRequisitionPrintViewModel` (Columns: `List<PrintColumn>`, Items) konsistent zwischen VM (Task 2), Controller (Task 3), View (Task 4), Tests (Task 3). View-Key-String `"WarehousePickingDetails"` identisch in ColumnDefinitions, Controller, Details.cshtml, Tests. `WarehouseRequisitionDetailItemViewModel`-Konstruktor-Reihenfolge (Id, Position, ArticleNumber, ArticleDescription, Unit, QuantityRequested, QuantityPicked, StorageLocations, Note, ShortageStatus, NoteEinkauf) wie in der bestehenden Codebase.
