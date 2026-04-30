# OSEON Tracking Article-Filter Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Artikel-Filter im OSEON-Tracking auf Server-side via Form-Submit umstellen — Performance-Fix (kein Browser-Freeze) + UX-Fix (Filter wirkt auf Gesamtbestand).

**Architecture:** Repository-Methode `GetPagedAsync` bekommt optionalen `articleNumber`-Parameter (Contains-Match auf `OseonProductionOrder.ArticleNumber`). Controller reicht `filterArticle` Query-Parameter durch. View integriert Input ins bestehende GET-Form, JS-Live-Filter wird ersatzlos entfernt. Neuer DB-Index `IX_OseonProductionOrders_ArticleNumber`.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10, SQL Server, xUnit + FluentAssertions + EF InMemory, Bootstrap 5.

**Scope:** Pfade relativ zu `C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting`, Branch `feature/oseon-reporting`. Versions-Bump v1.7.1 → v1.7.2.

---

## Task 1: Repository — `GetPagedAsync` um `articleNumber`-Parameter erweitern (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IOseonProductionOrderRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs`
- Create: `IdealAkeWms.Tests/Repositories/OseonProductionOrderRepositoryArticleFilterTests.cs`

**Wichtig:** `articleNumber` kommt als **letzter** Parameter mit Default `null` — damit alle 7 bestehenden positionellen Test-Calls in `OseonProductionOrderRepositoryTests.cs` (Zeilen 148/153/177/200/223/247/273) weiter kompilieren.

- [ ] **Step 1: Test-Datei mit 5 Tests anlegen**

`IdealAkeWms.Tests/Repositories/OseonProductionOrderRepositoryArticleFilterTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class OseonProductionOrderRepositoryArticleFilterTests
{
    private static OseonProductionOrder NewOrder(long oseonId, string? articleNumber,
        string custOrder = "K-100", string faNumber = "FA-100")
        => new()
        {
            OseonId = oseonId,
            OseonOrderNumber = faNumber,
            CustomerOrderNumber = custOrder,
            OseonStatus = 30,
            ArticleNumber = articleNumber,
            DueDate = DateTime.Today,
            LastChangedInOseon = DateTime.Now,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };

    [Fact]
    public async Task Filter_ByArticleNumber_ReturnsExactMatch()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OseonProductionOrders.Add(NewOrder(1, "ART-100", "K-1", "FA-1"));
        ctx.OseonProductionOrders.Add(NewOrder(2, "ART-999", "K-2", "FA-2"));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetPagedAsync(null, null, false, 1, 25, null, "ART-100");

        result.Items.Should().HaveCount(1);
        result.Items[0].ArticleNumber.Should().Be("ART-100");
    }

    [Fact]
    public async Task Filter_ByArticleNumber_ReturnsContainsMatch()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OseonProductionOrders.Add(NewOrder(1, "ART-100", "K-1", "FA-1"));
        ctx.OseonProductionOrders.Add(NewOrder(2, "ART-1001", "K-2", "FA-2"));
        ctx.OseonProductionOrders.Add(NewOrder(3, "OTHER", "K-3", "FA-3"));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetPagedAsync(null, null, false, 1, 25, null, "100");

        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.ArticleNumber).Should().BeEquivalentTo(new[] { "ART-100", "ART-1001" });
    }

    [Fact]
    public async Task Filter_ByArticleNumber_IgnoresOrdersWithNullArticleNumber()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OseonProductionOrders.Add(NewOrder(1, null, "K-1", "FA-1"));
        ctx.OseonProductionOrders.Add(NewOrder(2, "ART-100", "K-2", "FA-2"));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetPagedAsync(null, null, false, 1, 25, null, "ART");

        result.Items.Should().HaveCount(1);
        result.Items[0].ArticleNumber.Should().Be("ART-100");
    }

    [Fact]
    public async Task Filter_ByArticleNumber_WhitespaceOnly_TreatedAsNoFilter()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OseonProductionOrders.Add(NewOrder(1, "ART-A", "K-1", "FA-1"));
        ctx.OseonProductionOrders.Add(NewOrder(2, "ART-B", "K-2", "FA-2"));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetPagedAsync(null, null, false, 1, 25, null, "   ");

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Filter_ByArticleNumber_CombinedWithSearchTerm_AllConjunctive()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OseonProductionOrders.Add(NewOrder(1, "ART-100", "K-MATCH", "FA-1"));
        ctx.OseonProductionOrders.Add(NewOrder(2, "ART-100", "K-OTHER", "FA-2"));
        ctx.OseonProductionOrders.Add(NewOrder(3, "ART-999", "K-MATCH", "FA-3"));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetPagedAsync("K-MATCH", null, false, 1, 25, null, "ART-100");

        result.Items.Should().HaveCount(1);
        result.Items[0].OseonOrderNumber.Should().Be("FA-1");
    }
}
```

- [ ] **Step 2: Tests laufen → Compile-Fehler**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" && dotnet test --nologo --filter "FullyQualifiedName~OseonProductionOrderRepositoryArticleFilterTests" 2>&1 | tail -10
```

Expected: Build error — `GetPagedAsync` hat 7 statt 8 Argumente.

- [ ] **Step 3: Interface erweitern**

In `IdealAkeWms/Data/Repositories/IOseonProductionOrderRepository.cs` Methoden-Signatur ändern (am Ende der Parameter-Liste):

Vorher:
```csharp
Task<OseonPagedResult> GetPagedAsync(string? searchTerm, string? workplaceName, bool showFinished, int page, int pageSize, HashSet<string>? relevantOperationNames = null);
```

Nachher:
```csharp
Task<OseonPagedResult> GetPagedAsync(string? searchTerm, string? workplaceName, bool showFinished, int page, int pageSize, HashSet<string>? relevantOperationNames = null, string? articleNumber = null);
```

- [ ] **Step 4: Implementation erweitern**

In `IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs` die Methoden-Signatur (Zeile 36) entsprechend angleichen UND innerhalb der `baseQuery`-Aufbau-Logik (direkt nach dem `searchTerm`-Where-Block, vor `workplaceName`) einen neuen Block ergänzen:

```csharp
        if (!string.IsNullOrWhiteSpace(articleNumber))
        {
            var artTerm = articleNumber.Trim();
            baseQuery = baseQuery.Where(o => o.ArticleNumber != null
                && o.ArticleNumber.Contains(artTerm));
        }
```

Genaue Position: nach dem Block, der `searchTerm` verarbeitet (vorzugsweise direkt oberhalb des `workplaceName`-Filters — dort wo die anderen Where-Clauses kombiniert werden). Falls die Reihenfolge unklar ist: lies erst Zeilen ~36–60 von `OseonProductionOrderRepository.cs`, dann passende Stelle wählen.

- [ ] **Step 5: Tests laufen → grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" && dotnet test --nologo --filter "FullyQualifiedName~OseonProductionOrderRepositoryArticleFilterTests" 2>&1 | tail -10
cd "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Expected: 5/5 neue Tests grün, full suite weiterhin grün (336 + 5 = 341 erwartet — bestehende 7 positionelle Calls bleiben kompilierfähig).

- [ ] **Step 6: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" add IdealAkeWms/Data/Repositories/IOseonProductionOrderRepository.cs IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs IdealAkeWms.Tests/Repositories/OseonProductionOrderRepositoryArticleFilterTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" commit -m "feat(oseon): GetPagedAsync filters by ArticleNumber

Optionaler Parameter articleNumber (Contains-Match, am Ende der Signatur
fuer Backward-Compat). Null-/Whitespace wird als kein Filter behandelt;
Aufträge mit ArticleNumber=null werden bei aktivem Filter ausgeschlossen.
5 neue Tests."
```

---

## Task 2: Controller + ViewModel — `filterArticle` durchschleifen

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/OseonTrackingViewModel.cs`
- Modify: `IdealAkeWms/Controllers/TrackingController.cs`

- [ ] **Step 1: ViewModel erweitern**

In `IdealAkeWms/Models/ViewModels/OseonTrackingViewModel.cs` die Klasse `OseonTrackingViewModel` (Zeilen 5–18) um eine neue Property ergänzen — direkt nach `FilterCustomerOrder`:

Vorher:
```csharp
public string? FilterCustomerOrder { get; set; }
public string? FilterWorkplace { get; set; }
```

Nachher:
```csharp
public string? FilterCustomerOrder { get; set; }
public string? FilterArticle { get; set; }
public string? FilterWorkplace { get; set; }
```

- [ ] **Step 2: Controller-Parameter ergänzen**

In `IdealAkeWms/Controllers/TrackingController.cs` die `OseonIndex`-Action-Signatur (Zeile 185) erweitern:

Vorher:
```csharp
public async Task<IActionResult> OseonIndex(string? filterCustomerOrder, string? filterWorkplace, bool showFinished = false, bool useRelevanceFilter = true, int page = 1)
```

Nachher:
```csharp
public async Task<IActionResult> OseonIndex(string? filterCustomerOrder, string? filterArticle, string? filterWorkplace, bool showFinished = false, bool useRelevanceFilter = true, int page = 1)
```

- [ ] **Step 3: Repository-Call erweitern**

In derselben Action den `GetPagedAsync`-Aufruf (Zeile 204) anpassen:

Vorher:
```csharp
var pagedResult = await _oseonRepository.GetPagedAsync(filterCustomerOrder, filterWorkplace, showFinished, page, pageSize, relevantOpNames);
```

Nachher:
```csharp
var pagedResult = await _oseonRepository.GetPagedAsync(filterCustomerOrder, filterWorkplace, showFinished, page, pageSize, relevantOpNames, filterArticle);
```

- [ ] **Step 4: ViewModel-Construction erweitern**

Im selben Action-Body den ViewModel-Konstruktor (Zeile 326+) erweitern:

Vorher:
```csharp
var vm = new OseonTrackingViewModel
{
    OrderGroups = groups,
    FilterCustomerOrder = filterCustomerOrder,
    FilterWorkplace = filterWorkplace,
    ShowFinished = showFinished,
    ...
```

Nachher:
```csharp
var vm = new OseonTrackingViewModel
{
    OrderGroups = groups,
    FilterCustomerOrder = filterCustomerOrder,
    FilterArticle = filterArticle,
    FilterWorkplace = filterWorkplace,
    ShowFinished = showFinished,
    ...
```

- [ ] **Step 5: Build + Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Expected: 0 Errors, full suite grün.

- [ ] **Step 6: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" add IdealAkeWms/Models/ViewModels/OseonTrackingViewModel.cs IdealAkeWms/Controllers/TrackingController.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" commit -m "feat(oseon): TrackingController.OseonIndex routes filterArticle

ViewModel: neue Property FilterArticle (konsistent zu MovementHistory/StockOverview).
Action: filterArticle als optionaler Query-Parameter, durchgereicht
an Repository und zurueck ins ViewModel."
```

---

## Task 3: View — Form-Integration + JS-Cleanup

**Files:**
- Modify: `IdealAkeWms/Views/Tracking/OseonIndex.cshtml`

- [ ] **Step 1: Filter-Input ins Form holen + Name-Attribut + Value-Binding**

In `IdealAkeWms/Views/Tracking/OseonIndex.cshtml` Zeile 25 ändern.

Vorher:
```html
<input type="text" id="filterArticle" class="form-control form-control-sm" placeholder="Artikelnummer scannen..." />
```

Nachher:
```html
<input type="text" id="filterArticle" name="filterArticle" value="@Model.FilterArticle" class="form-control form-control-sm" placeholder="Artikelnummer..." />
```

- [ ] **Step 2: Reset-Link Bedingung erweitern**

In Zeile 61 (siehe `@if (!string.IsNullOrEmpty(Model.FilterCustomerOrder) ...)`) die Bedingung um den Artikel-Filter erweitern:

Vorher:
```cshtml
@if (!string.IsNullOrEmpty(Model.FilterCustomerOrder) || !string.IsNullOrEmpty(Model.FilterWorkplace) || Model.ShowFinished || !Model.UseRelevanceFilter)
```

Nachher:
```cshtml
@if (!string.IsNullOrEmpty(Model.FilterCustomerOrder) || !string.IsNullOrEmpty(Model.FilterArticle) || !string.IsNullOrEmpty(Model.FilterWorkplace) || Model.ShowFinished || !Model.UseRelevanceFilter)
```

- [ ] **Step 3: Empty-State-Hinweis um Artikel ergänzen (optional)**

In Zeilen 71–76 (Block "Keine OSEON-Aufträge gefunden"), falls die Bedingung „Filter aktiv" geprüft wird, ebenfalls `Model.FilterArticle` einbeziehen. Konkret:

Vorher (typischerweise):
```cshtml
@if (!string.IsNullOrEmpty(Model.FilterCustomerOrder) || !string.IsNullOrEmpty(Model.FilterWorkplace))
```

Nachher:
```cshtml
@if (!string.IsNullOrEmpty(Model.FilterCustomerOrder) || !string.IsNullOrEmpty(Model.FilterArticle) || !string.IsNullOrEmpty(Model.FilterWorkplace))
```

(Wenn die Stelle in der View anders aussieht, einfach belassen — diese Anpassung ist nur kosmetisch.)

- [ ] **Step 4: Inline-Style-Block entfernen**

Zeilen 348–350 komplett entfernen:

```html
<style>
    .article-filter-hidden { display: none !important; }
</style>
```

- [ ] **Step 5: Live-Filter-JS entfernen**

Zeilen 459–530 (von Kommentar `// === Artikelnummer-Filter (client-seitig) ===` bis einschließlich der schließenden `});` des `initTextInputScanner('btnScanArticle', ...)`-Blocks) komplett entfernen.

**Zu erhalten:** den anderen Scanner für `btnScanCustomerOrder` ab Zeile 532 — der bleibt unverändert.

**Zu ersetzen:** den Article-Scanner-Block durch eine analoge Form-Submit-Variante:

Direkt vor `initTextInputScanner('btnScanCustomerOrder', ...)` einfügen:

```javascript
// QR scanner for article filter — fills input then submits form
initTextInputScanner('btnScanArticle', 'filterArticle', 'article', function() {
    var form = document.getElementById('filterArticle').closest('form');
    if (form) form.submit();
});
```

- [ ] **Step 6: `data-article-number`-Attribute auf `<tr>`-Zeilen entfernen**

In `OseonIndex.cshtml` nach `data-article-number=` greppen:

```bash
grep -n 'data-article-number' "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting/IdealAkeWms/Views/Tracking/OseonIndex.cshtml"
```

Erwartet: nur noch Zeile 161 (Sub-Order-Render-Zeile). Den `data-article-number="@sub.ArticleNumber"`-Teil aus dem `<tr>`-Tag entfernen — die übrigen Attribute (`data-parent`, `data-sub`, `class`, `style`, etc.) bleiben.

Vorher (Zeile 161):
```html
<tr class="oseon-tree-sub @subRowClass" data-parent="@groupKey" data-sub="@subKey" data-article-number="@sub.ArticleNumber" style="display: none; cursor: pointer;">
```

Nachher:
```html
<tr class="oseon-tree-sub @subRowClass" data-parent="@groupKey" data-sub="@subKey" style="display: none; cursor: pointer;">
```

- [ ] **Step 7: Build + Razor-Compile + Smoke-grep**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" && dotnet build --nologo 2>&1 | tail -3
grep -n 'article-filter-hidden\|data-article-number\|applyArticleFilter' "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting/IdealAkeWms/Views/Tracking/OseonIndex.cshtml" || echo "all references removed"
cd "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Expected: 0 Errors, "all references removed", full suite grün.

- [ ] **Step 8: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" add IdealAkeWms/Views/Tracking/OseonIndex.cshtml
git -C "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" commit -m "fix(oseon): article filter via form-submit, remove client-live-filter

- Input ins GET-Form integriert (name=filterArticle, value=Model)
- QR-Scanner triggert Form-Submit (analog filterCustomerOrder)
- Inline-CSS .article-filter-hidden entfernt
- ~70 Zeilen Live-Filter-JS ersatzlos entfernt
- data-article-number Attribute auf Sub-Rows entfernt (toter Anchor)
- Reset-Link erkennt aktiven Article-Filter

Loest Browser-Freeze beim Tippen + UX-Mangel (Filter wirkte nur auf
aktuelle Seite). Server-Pagination greift jetzt auch fuer Artikel-Suche."
```

---

## Task 4: DB-Index — EF-Migration + SQL/50 + FreshInstall

**Files:**
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs`
- Create: `IdealAkeWms/Migrations/<timestamp>_AddOseonArticleNumberIndex.cs` (via EF CLI)
- Create: `SQL/50_AddOseonArticleNumberIndex.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: ApplicationDbContext-Index ergänzen**

In `IdealAkeWms/Data/ApplicationDbContext.cs` im `OseonProductionOrder`-Mapping-Block bei den anderen `HasIndex`-Aufrufen (nach Zeile 507 `entity.HasIndex(e => e.WorkplaceName);`):

```csharp
            entity.HasIndex(e => e.ArticleNumber);
```

- [ ] **Step 2: EF-Migration generieren**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting/IdealAkeWms" && dotnet ef migrations add AddOseonArticleNumberIndex
```

Erwartet: neue Datei `Migrations/<timestamp>_AddOseonArticleNumberIndex.cs`.

- [ ] **Step 3: Migration prüfen**

```bash
ls "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting/IdealAkeWms/Migrations/" | grep -i AddOseonArticleNumberIndex
cat "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting/IdealAkeWms/Migrations/"*_AddOseonArticleNumberIndex.cs
```

Expected `Up()`:
```csharp
migrationBuilder.CreateIndex(
    name: "IX_OseonProductionOrders_ArticleNumber",
    table: "OseonProductionOrders",
    column: "ArticleNumber");
```

`Down()` ruft `DropIndex` mit demselben Namen.

Falls unrelated Drift (andere Schema-Änderungen): Migration löschen, beheben, neu generieren.

- [ ] **Step 4: SQL/50 anlegen**

`SQL/50_AddOseonArticleNumberIndex.sql`:

```sql
-- Phase: OSEON Tracking Article Filter Fix v1.7.2
-- Idempotent: Index auf ArticleNumber für Filter-Performance.

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_OseonProductionOrders_ArticleNumber'
      AND object_id = OBJECT_ID('dbo.OseonProductionOrders'))
BEGIN
    CREATE INDEX IX_OseonProductionOrders_ArticleNumber
        ON dbo.OseonProductionOrders(ArticleNumber);
END
GO
```

Ergänze außerdem den EF-Migrations-History-Eintrag (separater Batch):

```sql
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId LIKE '%_AddOseonArticleNumberIndex')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('<MIGRATION_ID>_AddOseonArticleNumberIndex', '10.0.2');
END
GO
```

`<MIGRATION_ID>` durch den Timestamp-Präfix aus Step 3 ersetzen. ProductVersion an aktuellen Snapshot anpassen (`grep "ProductVersion" "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting/IdealAkeWms/Migrations/ApplicationDbContextModelSnapshot.cs" | head -1`).

- [ ] **Step 5: FreshInstall ergänzen**

In `SQL/00_FreshInstall.sql` direkt nach Zeile 379 (`CREATE INDEX [IX_OseonProductionOrders_OseonStatus]...`) und vor dem `END`-Block:

```sql
    CREATE INDEX [IX_OseonProductionOrders_ArticleNumber] ON [dbo].[OseonProductionOrders]([ArticleNumber]);
```

(Die genaue Indenting-Tiefe an die umliegenden CREATE-INDEX-Statements anpassen.)

- [ ] **Step 6: Build + Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Expected: 0 errors (kein "Pending model changes"-Warning), full suite grün.

- [ ] **Step 7: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" add IdealAkeWms/Data/ApplicationDbContext.cs IdealAkeWms/Migrations/ SQL/50_AddOseonArticleNumberIndex.sql SQL/00_FreshInstall.sql
git -C "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" commit -m "feat(oseon): add IX_OseonProductionOrders_ArticleNumber

EF migration AddOseonArticleNumberIndex + idempotenter SQL/50 +
FreshInstall-Eintrag. Vorbereitung fuer kuenftige StartsWith-/Equality-
Suche; Contains nutzt den Index begrenzt (Index-Scan statt Full-Scan)."
```

---

## Task 5: AppVersion + Help + Changelog + PROJECT_STATUS + TESTSZENARIEN

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `PROJECT_STATUS.md`
- Modify: `docs/TESTSZENARIEN.md`

- [ ] **Step 1: AppVersion bump (beide Projekte)**

In `IdealAkeWms/AppVersion.cs`:

```csharp
public const string Version = "1.7.2";
public const string Date = "2026-04-30";
```

In `IDEALAKEWMSService/AppVersion.cs` identisch.

- [ ] **Step 2: Help/Index.cshtml — Tracking-Filter um Artikel ergänzen**

In `IdealAkeWms/Views/Help/Index.cshtml` zwischen Zeilen 262–272 die Filter-Liste der OSEON-Tracking-Section um Artikelnummer erweitern. Konkret im `<dl>` (oder analog zur dort genutzten Liste) folgende Einträge ergänzen — Position direkt nach `Auftragsnummer`/`Kundenauftragsnummer`:

```html
<dt>Artikelnummer</dt>
<dd>Server-seitige Suche (Contains-Match) auf der OSEON-Auftrags-Artikelnummer. QR-Scan eines Artikel-Codes löst Form-Submit aus. Ergebnis ist paginiert über den Gesamtbestand.</dd>
```

(Wenn die Help-Section eine andere Markup-Form verwendet, der bestehenden Struktur angleichen.)

- [ ] **Step 3: Changelog.cshtml — v1.7.2 Eintrag**

In `IdealAkeWms/Views/Help/Changelog.cshtml` über dem v1.7.1-Block einen neuen v1.7.2-Block einfügen. Falls der Changelog Card-basiertes Markup verwendet, dem Pattern folgen. Inhalt:

```html
<h5>v1.7.2 &mdash; 30.04.2026</h5>
<ul>
    <li><strong>OSEON Tracking Artikel-Filter:</strong> Server-seitige Suche statt Browser-Live-Filter. Behebt App-Freeze beim Tippen und macht den Filter über den Gesamtbestand wirksam.</li>
    <li>Neuer DB-Index <code>IX_OseonProductionOrders_ArticleNumber</code> als Performance-Vorbereitung.</li>
    <li>Repository <code>GetPagedAsync</code> akzeptiert optionalen <code>articleNumber</code>-Parameter (Contains, Null-safe).</li>
    <li>QR-Scan eines Artikel-Codes löst nun Form-Submit aus (analog Auftragsnummer-Scan).</li>
</ul>
```

- [ ] **Step 4: PROJECT_STATUS.md — v1.7.2 Eintrag**

In `PROJECT_STATUS.md` über dem v1.7.1-Block einen neuen v1.7.2-Eintrag mit Bullet-Liste der Hauptfeatures und Datum 2026-04-30 ergänzen. Versions-Header aktualisieren falls dort separat ausgewiesen.

- [ ] **Step 5: TESTSZENARIEN.md — Bereich 2 anlegen**

In `docs/TESTSZENARIEN.md` neuen Abschnitt anhängen:

```markdown
## 2. OSEON Tracking — Artikel-Filter

### TS-2.1 — Artikelnummer-Filter via Form-Submit
**Vorbedingungen:** Tracking-Rolle, mindestens 3 OSEON-Aufträge mit unterschiedlichen Artikelnummern, davon mindestens einer mit Artikelnummer-Pattern "ART-100*".
**Schritte:**
1. /Tracking/OseonIndex öffnen.
2. In das Feld "Artikelnummer" `100` eingeben.
3. Auf "Filtern" klicken.
**Erwartet:** Ergebnis enthaelt nur Customer-Order-Gruppen mit mindestens einem Sub-Auftrag dessen ArticleNumber `100` enthaelt. Innerhalb einer matchenden Gruppe werden ALLE Sub-Auftraege angezeigt (auch nicht-matchende — siehe Spec §4 Group-Pagination). Filter-Wert bleibt im Input. Pagination wirkt auf gefiltertes Group-Ergebnis.

### TS-2.2 — Kombinierter Filter (Artikel + Werkbank + Auftrag)
**Vorbedingungen:** Daten mit verschiedenen Werkbaenken + Artikelnummern.
**Schritte:**
1. Artikelnummer eingeben + Werkbank waehlen + Auftragsnummer-Suchterm eingeben.
2. "Filtern" klicken.
**Erwartet:** Schnittmenge aller Filter (konjunktiv). Reset-Link sichtbar.

### TS-2.3 — QR-Scan triggert Form-Submit
**Schritte:**
1. Auf den QR-Button neben dem Artikelnummer-Input klicken.
2. Artikel-QR-Code scannen.
**Erwartet:** Input wird mit dem gescannten Wert befuellt UND Form wird automatisch submitted (Page-Reload mit Filter aktiv).

### TS-2.4 — Reset entfernt alle Filter inkl. Artikel
**Vorbedingungen:** Mindestens ein Filter ist aktiv (Artikel, Auftrag, Werkbank, ShowFinished oder useRelevanceFilter=false).
**Schritte:**
1. "Zuruecksetzen"-Link klicken.
**Erwartet:** Alle Filter zurueckgesetzt (Artikel-Input leer), volle Liste sichtbar.
```

Falls die TESTSZENARIEN.md eine Stand-Zeile am Anfang hat, diese auf `**Stand:** 2026-04-30 (v1.7.2)` aktualisieren.

- [ ] **Step 6: Build + Final Test**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Expected: 0 errors, full suite weiterhin grün.

- [ ] **Step 7: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/ PROJECT_STATUS.md docs/TESTSZENARIEN.md
git -C "C:/Git/IDEAL-AKE-WMS_WT_oseon-reporting" commit -m "chore(oseon): v1.7.2 docs + TESTSZENARIEN

Help-Index: Artikel-Filter dokumentiert.
Changelog v1.7.2 (30.04.2026).
PROJECT_STATUS v1.7.2 Eintrag.
TESTSZENARIEN: neuer Bereich 2 (TS-2.1..2.4).
AppVersion bump 1.7.1 -> 1.7.2 (Web + Service)."
```

---

## Final Summary

5 Tasks, ca. 5 neue Unit-Tests + 4 manuelle Szenarien. Versions-Bump 1.7.1 → 1.7.2 auf `feature/oseon-reporting`.

### Self-Review

1. **Spec coverage:**
   - §3 Lösungsansatz → Tasks 1+2+3+4
   - §4 AG-Level-Filter-Verhalten → wird automatisch durch Server-Filter erfüllt (kein eigener Task nötig)
   - §5 Komponenten → alle Tasks
   - §6 Repository-Signatur (articleNumber am Ende) → Task 1
   - §7 View-Änderung → Task 3
   - §8 UX-Trade-off → in Spec dokumentiert, in TS-2.3 manuell verifizierbar
   - §9 DB-Index → Task 4
   - §10 Tests → Task 1 (Unit) + Task 5 (Manual)
   - §11 Out of Scope → keine Task
   - §12 Risiken → in Tasks adressiert (EF-Migration in Task 4 Step 3, Backward-Compat in Task 1, Null-Check in Task 1 Step 4)

2. **Placeholder scan:** Keine TBDs/TODOs. Alle Code-Blöcke vollständig.

3. **Type consistency:**
   - `articleNumber` (Parameter) konsistent zwischen Interface, Implementation, Tests, Controller-Call.
   - `filterArticle` (Query-Param-Name + ViewModel-Property `FilterArticle`) konsistent zwischen Controller, ViewModel, View.
   - Method-Signaturen identisch zwischen Interface (Task 1 Step 3) und Implementation (Task 1 Step 4).
   - Migration-Name `AddOseonArticleNumberIndex` konsistent zwischen EF (Task 4 Step 2), SQL (Step 4), Migrations-History-Insert (Step 4).
   - Index-Name `IX_OseonProductionOrders_ArticleNumber` konsistent zwischen DbContext-HasIndex (Step 1), Migration (Step 3), SQL/50 (Step 4), FreshInstall (Step 5).
