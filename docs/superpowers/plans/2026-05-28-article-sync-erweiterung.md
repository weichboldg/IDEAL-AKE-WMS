# Artikel-Sync-Erweiterung Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `SageImportService.SyncArticlesAsync` erweitern um aktive Bestellartikel (UNION mit IstBestellartikel=-1 AND Aktiv=-1) und Meldebestand-Uebernahme aus `KHKArtikelvarianten`, mit Full-Update-UPSERT.

**Architecture:** Ein einziger Service-Method-Refactor (Read aus Sage, Write nach WMS) plus extrahierte Parsing-Helper (`SageImportHelpers.ParseReorderLevel`). Bestehende SyncLog-Lifecycle (`BeginRunAsync` → `FinishSuccessAsync`/`FinishFailedAsync`) bleibt. AgentJob-Skript `02_Import_Artikel.sql` wird mit Deprecation-Header ausgemustert.

**Tech Stack:** C# / .NET 10, raw ADO.NET (Microsoft.Data.SqlClient), xUnit + FluentAssertions, SQL Server 2019+

**Worktree:** `.claude/worktrees/article-sync-erweiterung`, Branch `feature/article-sync-erweiterung`

**Spec:** [secondbrain/docs/superpowers/specs/2026-05-28-article-sync-erweiterung-design.md](../specs/2026-05-28-article-sync-erweiterung-design.md) (Commit 1108c0a)

---

## File Structure

**Create:**
- `IDEALAKEWMSService/Services/SageImportHelpers.cs` — `internal static` Parse-Helper, eigene Datei damit klar testbar

**Modify:**
- `IDEALAKEWMSService/IDEALAKEWMSService.csproj` — `InternalsVisibleTo("IDEALAKEWMSService.Tests")` ergaenzen
- `IDEALAKEWMSService/Services/SageImportService.cs` — `SyncArticlesAsync` (Sage-SQL + Write-Phase + Counts)
- `IDEALAKEWMSService.Tests/Services/SageImportServiceTests.cs` — neue `ParseReorderLevel`-Theory hinzu
- `SQL/AgentJobs/02_Import_Artikel.sql` — Deprecation-Header oben drueber
- `IdealAkeWms/AppVersion.cs` — Version "1.17.0"
- `IDEALAKEWMSService/AppVersion.cs` — Version "1.17.0" (war auf 1.15.3 — zieht nach)
- `IdealAkeWms/Views/Help/Changelog.cshtml` — neuer v1.17.0-Card prependet
- `PROJECT_STATUS.md` — neue v1.17.0-Sub-Task-Tabelle prependet
- `docs/TESTSZENARIEN.md` — Kapitel 31 angefuegt
- `CLAUDE.md` — neuer Fallstrick zu Sage VB6-Boolean

---

## Task 0: Pre-Flight Baseline

**Files:** keine Aenderungen

- [ ] **Step 1: Im Worktree pruefen**

```bash
git rev-parse --abbrev-ref HEAD
# Expected: feature/article-sync-erweiterung

git log --oneline -3
# Expected: 1108c0a docs(spec): Artikel-Sync-Erweiterung ...
#           29befc2 merge bugfix/oseon-tracking-ios into main (v1.16.0)
```

- [ ] **Step 2: Baseline-Build**

```bash
dotnet build IdealAkeWms.sln
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Baseline-Tests**

```bash
dotnet test IdealAkeWms.sln --no-build
```

Expected: alle Tests gruen (590 Web + 91 Service = 681 oder gleich aktueller Wert auf main).

- [ ] **Step 4: SageImportService.cs Zeilen 264-368 lesen + bestaetigen**

```bash
sed -n '264,368p' IDEALAKEWMSService/Services/SageImportService.cs
```

Expected: `SyncArticlesAsync(bool dryRun, CancellationToken ct)` Methode mit `BeginRunAsync(SyncLogServices.Article, ct)`.

---

## Task 1: InternalsVisibleTo im Service-Projekt

**Files:**
- Modify: `IDEALAKEWMSService/IDEALAKEWMSService.csproj`

- [ ] **Step 1: csproj-Aenderung**

In `IDEALAKEWMSService/IDEALAKEWMSService.csproj` direkt nach der bestehenden `<ItemGroup>` mit `PackageReference`-Eintraegen (vor dem `<ItemGroup>` mit `ProjectReference`) eine neue `<ItemGroup>` einfuegen:

```xml
  <ItemGroup>
    <InternalsVisibleTo Include="IDEALAKEWMSService.Tests" />
  </ItemGroup>
```

- [ ] **Step 2: Build verifizieren**

```bash
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add IDEALAKEWMSService/IDEALAKEWMSService.csproj
git commit -m "chore(service): expose internals to IDEALAKEWMSService.Tests

Vorbereitung fuer SageImportHelpers-Unit-Tests.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: SageImportHelpers.ParseReorderLevel (TDD)

**Files:**
- Create: `IDEALAKEWMSService/Services/SageImportHelpers.cs`
- Modify: `IDEALAKEWMSService.Tests/Services/SageImportServiceTests.cs`

- [ ] **Step 1: Failing Test schreiben**

In `IDEALAKEWMSService.Tests/Services/SageImportServiceTests.cs` am Ende der Klasse (vor schliessendem `}`) einfuegen:

```csharp
    [Theory]
    [InlineData(null,        null)]
    [InlineData("",          null)]
    [InlineData("   ",       null)]
    [InlineData("0",         null)]
    [InlineData("0.0000",    null)]
    [InlineData("10.5",      "10.5")]
    [InlineData("abc",       null)]
    [InlineData("-5",        "-5")]
    public void ParseReorderLevel_handles_edge_cases(string? raw, string? expected)
    {
        var result = SageImportHelpers.ParseReorderLevel(raw);
        if (expected is null)
            result.Should().BeNull();
        else
            result.Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }
```

Auch in den `using`-Block oben in der Datei diese Zeile sicherstellen (falls noch nicht da):

```csharp
using IDEALAKEWMSService.Services;
```

(Sollte schon vorhanden sein, da `SageImportService` aus diesem Namespace kommt.)

- [ ] **Step 2: Test laufen, sicherstellen dass er FAILT**

```bash
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~ParseReorderLevel"
```

Expected: Build fail — `error CS0103: The name 'SageImportHelpers' does not exist`

- [ ] **Step 3: Helper-Klasse anlegen**

Neue Datei `IDEALAKEWMSService/Services/SageImportHelpers.cs`:

```csharp
using System.Globalization;

namespace IDEALAKEWMSService.Services;

internal static class SageImportHelpers
{
    /// <summary>
    /// Parsed den als nvarchar(20) gelieferten Meldebestand-Wert aus Sage.
    /// </summary>
    /// <remarks>
    /// Regeln: NULL/Empty/Whitespace -> null. Parse-Fehler still als null.
    /// Erkannter Wert 0 -> null (Sage liefert "0" oder "0.0000" fuer "kein Meldebestand").
    /// </remarks>
    internal static decimal? ParseReorderLevel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)) return null;
        return value == 0m ? null : value;
    }
}
```

- [ ] **Step 4: Test laufen, sicherstellen dass er PASST**

```bash
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~ParseReorderLevel"
```

Expected: `Passed!  - Failed: 0, Passed: 8, Skipped: 0`

- [ ] **Step 5: Commit**

```bash
git add IDEALAKEWMSService/Services/SageImportHelpers.cs IDEALAKEWMSService.Tests/Services/SageImportServiceTests.cs
git commit -m "feat(service): add SageImportHelpers.ParseReorderLevel

Parsed nvarchar(20)-Meldebestand aus Sage zu decimal? (InvariantCulture).
Regeln: NULL/Empty -> null, Parse-Fehler -> null, 0 -> null.
8 Theory-Tests fuer Edge-Cases.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: SageImportService.SyncArticlesAsync umstellen

**Files:**
- Modify: `IDEALAKEWMSService/Services/SageImportService.cs` (Methode `SyncArticlesAsync`, ca. Zeile 264-368)

- [ ] **Step 1: Sage-SQL ersetzen**

In `SageImportService.cs` die `const string sageSql = ...`-Konstante in `SyncArticlesAsync` (aktuell Zeile 277-286) komplett ersetzen durch:

```csharp
            const string sageSql = """
                WITH RawArticles AS (
                    SELECT DISTINCT
                        CAST(r.Ressourcenummer AS nvarchar(100))   AS ArticleNumber,
                        CAST(a.Bezeichnung1 AS nvarchar(500))      AS Description,
                        CAST(a.Lagermengeneinheit AS nvarchar(20)) AS Unit,
                        CAST(a.Artikelgruppe AS nvarchar(100))     AS ArticleGroup,
                        CAST(v.Meldebestand AS nvarchar(20))       AS ReorderLevel
                    FROM [dbo].[KHKPpsRessourcenPositionen] r
                    LEFT JOIN [dbo].[KHKArtikel] a ON a.Artikelnummer = r.Ressourcenummer
                    LEFT JOIN [dbo].[KHKArtikelvarianten] v ON a.Artikelnummer = v.Artikelnummer
                    WHERE r.Ressourcenummer IS NOT NULL AND r.Ressourcenummer != ''

                    UNION

                    SELECT
                        CAST(a.Artikelnummer AS nvarchar(100))     AS ArticleNumber,
                        CAST(a.Bezeichnung1 AS nvarchar(500))      AS Description,
                        CAST(a.Lagermengeneinheit AS nvarchar(20)) AS Unit,
                        CAST(a.Artikelgruppe AS nvarchar(100))     AS ArticleGroup,
                        CAST(v.Meldebestand AS nvarchar(20))       AS ReorderLevel
                    FROM [dbo].[KHKArtikel] a
                    LEFT JOIN [dbo].[KHKArtikelvarianten] v ON a.Artikelnummer = v.Artikelnummer
                    WHERE a.IstBestellartikel = -1 AND a.Aktiv = -1
                )
                SELECT
                    ArticleNumber,
                    MAX(Description)  AS Description,
                    MAX(Unit)         AS Unit,
                    MAX(ArticleGroup) AS ArticleGroup,
                    MAX(ReorderLevel) AS ReorderLevel
                FROM RawArticles
                WHERE ArticleNumber IS NOT NULL AND ArticleNumber != ''
                GROUP BY ArticleNumber
                """;
```

- [ ] **Step 2: Tuple um ReorderLevel erweitern + Reader anpassen**

Die Zeile

```csharp
            var sageArticles = new List<(string ArticleNumber, string? Description, string? Unit, string? ArticleGroup)>();
```

ersetzen durch

```csharp
            var sageArticles = new List<(string ArticleNumber, string? Description, string? Unit, string? ArticleGroup, decimal? ReorderLevel)>();
```

Und den `while`-Block direkt darunter ersetzen durch:

```csharp
                while (await reader.ReadAsync(ct))
                {
                    string? reorderRaw = reader.IsDBNull(4) ? null : reader.GetString(4);
                    sageArticles.Add((
                        ArticleNumber: reader.GetString(0),
                        Description:   reader.IsDBNull(1) ? null : reader.GetString(1),
                        Unit:          reader.IsDBNull(2) ? null : reader.GetString(2),
                        ArticleGroup:  reader.IsDBNull(3) ? null : reader.GetString(3),
                        ReorderLevel:  SageImportHelpers.ParseReorderLevel(reorderRaw)
                    ));
                }
```

- [ ] **Step 3: Write-Phase auf Full-Update-UPSERT umstellen**

Die bestehende Write-Phase (aktuell der Block ab `int inserted = 0;` bis zu `if (result is int i && i == 1) inserted++;`) ersetzen durch:

```csharp
            int inserted = 0, updated = 0;

            await using var wmsConn = new SqlConnection(wmsConnection);
            await wmsConn.OpenAsync(ct);

            foreach (var article in sageArticles)
            {
                const string upsertSql = """
                    IF EXISTS (SELECT 1 FROM [dbo].[Articles] WHERE [ArticleNumber] = @ArticleNumber)
                    BEGIN
                        UPDATE [dbo].[Articles] SET
                            [Description]       = @Description,
                            [Unit]              = @Unit,
                            [ArticleGroup]      = @ArticleGroup,
                            [ReorderLevel]      = @ReorderLevel,
                            [ModifiedAt]        = GETUTCDATE(),
                            [ModifiedBy]        = 'IDEALAKEWMSService',
                            [ModifiedByWindows] = SYSTEM_USER
                        WHERE [ArticleNumber] = @ArticleNumber
                          AND (
                              ISNULL([Description],'')   != ISNULL(@Description,'')   OR
                              ISNULL([Unit],'')          != ISNULL(@Unit,'')          OR
                              ISNULL([ArticleGroup],'')  != ISNULL(@ArticleGroup,'')  OR
                              ISNULL([ReorderLevel],-1)  != ISNULL(@ReorderLevel,-1)
                          )
                        SELECT 0 AS IsInsert, @@ROWCOUNT AS Affected
                    END
                    ELSE
                    BEGIN
                        INSERT INTO [dbo].[Articles]
                            ([ArticleNumber],[Description],[Unit],[ArticleGroup],[ReorderLevel],
                             [CreatedAt],[CreatedBy],[CreatedByWindows])
                        VALUES (@ArticleNumber, @Description, @Unit, @ArticleGroup, @ReorderLevel,
                                GETUTCDATE(), 'IDEALAKEWMSService', SYSTEM_USER)
                        SELECT 1 AS IsInsert, 1 AS Affected
                    END
                    """;

                await using var cmd = new SqlCommand(upsertSql, wmsConn);
                cmd.Parameters.AddWithValue("@ArticleNumber", article.ArticleNumber);
                cmd.Parameters.AddWithValue("@Description",  (object?)article.Description  ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Unit",         (object?)article.Unit         ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ArticleGroup", (object?)article.ArticleGroup ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ReorderLevel", (object?)article.ReorderLevel ?? DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    var isInsert = reader.GetInt32(0) == 1;
                    var affected = reader.GetInt32(1);
                    if (isInsert) inserted++;
                    else if (affected > 0) updated++;
                }
            }

            _logger.LogInformation("Artikel-Sync abgeschlossen: {Inserted} neu, {Updated} aktualisiert.", inserted, updated);
```

- [ ] **Step 4: DryRun-Pfad + FinishSuccess-Counts erweitern**

Den DryRun-Block ersetzen durch:

```csharp
            if (dryRun)
            {
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["gelesen"]      = sageArticles.Count,
                    ["neu"]          = 0,
                    ["aktualisiert"] = 0,
                }, messageSuffix: "[DryRun]", ct: ct);
                return new SyncResult(0, 0, 0, $"DryRun: {sageArticles.Count} Datensätze aus SAGE gelesen.");
            }
```

Und den `FinishSuccessAsync`-Aufruf am Ende der Try-Phase ersetzen durch:

```csharp
            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["gelesen"]      = sageArticles.Count,
                ["neu"]          = inserted,
                ["aktualisiert"] = updated,
            }, ct: ct);

            return new SyncResult(inserted, updated, 0);
```

- [ ] **Step 5: Build + bestehende Tests**

```bash
dotnet build IdealAkeWms.sln
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

```bash
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --no-build
```

Expected: alle bisherigen Service-Tests gruen + die 8 neuen ParseReorderLevel-Tests. `SyncArticlesAsync_writes_lifecycle_via_failure_path` muss weiter passieren.

- [ ] **Step 6: Commit**

```bash
git add IDEALAKEWMSService/Services/SageImportService.cs
git commit -m "feat(sync): UNION mit IstBestellartikel + Meldebestand + Full-Update

SyncArticlesAsync:
- Sage-SQL: CTE mit UNION + LEFT JOIN KHKArtikelvarianten + GROUP BY ArticleNumber + MAX()
- Liest jetzt zusaetzlich aktive Bestellartikel (IstBestellartikel = -1 AND Aktiv = -1)
- Meldebestand wird via SageImportHelpers.ParseReorderLevel uebernommen
- Write-Phase: Full-Update-UPSERT (Description/Unit/ArticleGroup/ReorderLevel) mit Audit-Feldern
- Counts: neu + aktualisiert (vorher nur neu)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: AgentJob-Skript Deprecation-Header

**Files:**
- Modify: `SQL/AgentJobs/02_Import_Artikel.sql`

- [ ] **Step 1: Header oben drueber einfuegen**

Den bestehenden Kopf-Kommentar-Block (Zeilen 1-14) ersetzen durch:

```sql
-- =============================================
-- DEPRECATED seit v1.17.0
-- Produktiver Sync laueft im IDEALAKEWMSService
-- (Sync:ArticlesEnabled = true). Dieses Skript ist nur
-- noch fuer manuelle Failover-Laeufe gedacht und nicht
-- mehr feature-vollstaendig:
--   - kein UNION mit IstBestellartikel/Aktiv-Filter
--   - keine Meldebestand-Uebernahme aus KHKArtikelvarianten
--   - kein Update bestehender Artikel
-- Bei Aenderungen am Service-Sync bitte hier NICHT nachziehen.
-- =============================================
-- SQL Server Agent Job: Artikel aus Sage importieren
-- Ziel:    [IDEAL_AKE_WMS].[dbo].[Articles]
-- Quelle:  [ake].[dbo].[KHKPpsRessourcenPositionen]
--          [ake].[dbo].[KHKArtikel]
--
-- Beschreibung:
--   Importiert neue Artikel (Bauteile/Ressourcen) aus Sage.
--   Bereits vorhandene Artikel (anhand ArticleNumber) werden uebersprungen.
--   Empfohlenes Intervall: taeglich oder stuendlich per SQL Server Agent.
--
-- Felder der Zieltabelle die NICHT befuellt werden (haben Defaults/NULL):
--   ReorderLevel     → NULL
-- =============================================
```

Body unveraendert lassen.

- [ ] **Step 2: Commit**

```bash
git add SQL/AgentJobs/02_Import_Artikel.sql
git commit -m "docs(sql): mark 02_Import_Artikel.sql as deprecated since v1.17.0

Produktiver Pfad ist IDEALAKEWMSService.SyncArticlesAsync.
Skript bleibt fuer manuelle Failover-Laeufe.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: AppVersion-Bump + Changelog

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`

- [ ] **Step 1: Web-AppVersion auf 1.17.0**

In `IdealAkeWms/AppVersion.cs`:

```csharp
namespace IdealAkeWms;

public static class AppVersion
{
    public const string Version = "1.17.0";
    public const string Date = "2026-05-28";
}
```

- [ ] **Step 2: Service-AppVersion auf 1.17.0**

In `IDEALAKEWMSService/AppVersion.cs` (war auf 1.15.3 — zieht nach):

```csharp
namespace IDEALAKEWMSService;

public static class AppVersion
{
    public const string Version = "1.17.0";
    public const string Date = "2026-05-28";
}
```

- [ ] **Step 3: Changelog v1.17.0-Card prependen**

In `IdealAkeWms/Views/Help/Changelog.cshtml` direkt nach Zeile 8 (`<div class="row"> <div class="col-lg-8">`) und VOR dem bestehenden v1.16.0-Card einfuegen:

```html
        <div class="card mb-3">
            <div class="card-header text-white" style="background-color: var(--ake-primary);">
                <strong>v1.17.0</strong> <span class="text-white-50 ms-2">28.05.2026</span>
            </div>
            <div class="card-body">
                <h6>Artikel-Sync erweitert: aktive Bestellartikel + Meldebestand</h6>
                <ul>
                    <li><strong>Mehr Artikel:</strong> Der Sage-Artikel-Sync liest jetzt zusaetzlich alle
                        aktiven Bestellartikel (<code>IstBestellartikel = -1 AND Aktiv = -1</code>) aus Sage,
                        nicht mehr nur Artikel die in einer Stueckliste auftauchen. Dadurch sind auch
                        Bestellartikel ohne BOM-Beteiligung im WMS verfuegbar (Voraussetzung fuer Bestand,
                        Lagerbestellungen, Meldebestand-Ampeln).</li>
                    <li><strong>Meldebestand-Uebernahme:</strong> Der in Sage gepflegte Meldebestand
                        (<code>KHKArtikelvarianten.Meldebestand</code>) wird beim Sync uebernommen.
                        Sage-Wert 0 wird als &quot;kein Meldebestand&quot; behandelt (NULL).</li>
                    <li><strong>Full-Update bei bestehenden Artikeln:</strong> Bisher wurde bei bekannten
                        Artikeln nur die Artikelgruppe aktualisiert. Ab jetzt aktualisiert der Sync auch
                        Bezeichnung, Einheit und Meldebestand wenn Sage Aenderungen liefert. Sage ist
                        Master-Datenquelle.</li>
                    <li><em>AgentJob-Skript:</em> <code>SQL/AgentJobs/02_Import_Artikel.sql</code> ist
                        seit dieser Version als <strong>DEPRECATED</strong> markiert und nicht mehr
                        feature-vollstaendig. Der produktive Sync laueft ausschliesslich ueber den
                        <code>IDEALAKEWMSService</code>.</li>
                </ul>
            </div>
        </div>

```

- [ ] **Step 4: Build + Tests**

```bash
dotnet build IdealAkeWms.sln
```

Expected: `Build succeeded.`

```bash
dotnet test IdealAkeWms.sln --no-build
```

Expected: alle Tests gruen.

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml
git commit -m "feat(version): bump to v1.17.0 (artikel-sync UNION + Meldebestand)

Web + Service AppVersion auf 1.17.0. Service war auf 1.15.3 — zieht
gleichzeitig nach (Drift war seit v1.16.0 unbeabsichtigt). Changelog-Card
in Help/Changelog.cshtml.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: TESTSZENARIEN Kapitel 31

**Files:**
- Modify: `docs/TESTSZENARIEN.md`

- [ ] **Step 1: Pruefen wo aktuell das letzte Kapitel endet**

```bash
grep -n "^## " docs/TESTSZENARIEN.md | tail -5
```

Expected: zeigt unter anderem `## 30. ...` (das v1.16.0-Kapitel). Wenn 30 das letzte ist, neues Kapitel 31 anhaengen.

- [ ] **Step 2: Kapitel 31 ans Ende anhaengen**

Am Ende von `docs/TESTSZENARIEN.md` (nach der letzten Zeile) anfuegen:

```markdown

---

## 31. Artikel-Sync-Erweiterung (v1.17.0)

Diese Szenarien decken die erweiterte Sage-Artikel-Synchronisation ab: UNION mit aktiven Bestellartikeln, Meldebestand-Uebernahme aus `KHKArtikelvarianten` und Full-Update bei bestehenden Artikeln.

### 31.1 Neuer Bestellartikel ohne BOM-Beteiligung kommt an

**Vorbedingungen:**
- Test-Artikel `TEST-9999` existiert in Sage `KHKArtikel` mit `IstBestellartikel = -1` und `Aktiv = -1`.
- `TEST-9999` taucht NICHT in `KHKPpsRessourcenPositionen` auf.
- `TEST-9999` ist im WMS noch nicht in der `Articles`-Tabelle.

**Schritte:**
1. Service-Lauf manuell triggern (Service-Restart oder warten auf naechsten Tick).
2. Aktivitaets-Protokoll (`/SyncLog/Index`) oeffnen, Filter Service = `Article`.
3. WMS-DB pruefen: `SELECT * FROM Articles WHERE ArticleNumber = 'TEST-9999'`.

**Erwartetes Verhalten:**
- Aktivitaets-Protokoll-Eintrag mit Counts inkl. `neu >= 1`.
- `Articles.TEST-9999` ist eingefuegt, `CreatedBy = 'IDEALAKEWMSService'`, `CreatedAt` ≈ jetzt.

### 31.2 Meldebestand-Uebernahme

**Vorbedingungen:**
- `TEST-9999` in `KHKArtikelvarianten.Meldebestand = 25.0000`.

**Schritte:**
1. Service-Sync triggern.
2. `SELECT ReorderLevel FROM Articles WHERE ArticleNumber = 'TEST-9999'`.

**Erwartetes Verhalten:**
- `ReorderLevel = 25.0000`.

### 31.3 Meldebestand 0 wird zu NULL

**Vorbedingungen:**
- `TEST-9998` in `KHKArtikel` (mit `IstBestellartikel = -1, Aktiv = -1`) und `KHKArtikelvarianten.Meldebestand = 0`.

**Schritte:**
1. Service-Sync triggern.
2. `SELECT ReorderLevel FROM Articles WHERE ArticleNumber = 'TEST-9998'`.

**Erwartetes Verhalten:**
- `ReorderLevel IS NULL` (nicht 0).

### 31.4 Full-Update bei bestehendem Artikel

**Vorbedingungen:**
- `TEST-9999` existiert bereits im WMS mit `Description = 'alt'`.
- In Sage wurde `KHKArtikel.Bezeichnung1` fuer `TEST-9999` auf `'neu'` geaendert.

**Schritte:**
1. Service-Sync triggern.
2. `SELECT Description, ModifiedAt, ModifiedBy FROM Articles WHERE ArticleNumber = 'TEST-9999'`.

**Erwartetes Verhalten:**
- `Description = 'neu'`.
- `ModifiedAt` ≈ jetzt.
- `ModifiedBy = 'IDEALAKEWMSService'`.
- Aktivitaets-Protokoll-Eintrag mit `aktualisiert >= 1`.

### 31.5 Idempotenz: Zweiter Lauf ohne Aenderungen schreibt nichts

**Vorbedingungen:**
- Sync wurde gerade gelaufen, keine weiteren Aenderungen in Sage.

**Schritte:**
1. Service-Sync sofort nochmal triggern.
2. Aktivitaets-Protokoll-Eintrag pruefen.

**Erwartetes Verhalten:**
- Counts: `neu = 0`, `aktualisiert = 0`, `gelesen > 0`.

### 31.6 DryRun-Modus

**Vorbedingungen:**
- AppSetting `WorkerSettings:SyncDryRun = true` (per `appsettings.json` oder `ServiceSettings`-Tabelle).
- Service neu starten damit Config gelesen wird.

**Schritte:**
1. Service-Sync triggern.
2. Aktivitaets-Protokoll pruefen.
3. WMS-Artikel pruefen: `SELECT COUNT(*) FROM Articles WHERE ModifiedAt > [letzter-Sync]`.

**Erwartetes Verhalten:**
- Aktivitaets-Protokoll-Eintrag mit Suffix `[DryRun]` in der Message und `neu = 0, aktualisiert = 0, gelesen = N`.
- Keine `Articles`-Aenderungen.

```

- [ ] **Step 3: Commit**

```bash
git add docs/TESTSZENARIEN.md
git commit -m "docs(test): TESTSZENARIEN Kapitel 31 fuer Artikel-Sync v1.17.0

6 manuelle Szenarien: neuer Bestellartikel, Meldebestand-Uebernahme,
0-als-NULL, Full-Update, Idempotenz, DryRun.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: PROJECT_STATUS + CLAUDE.md

**Files:**
- Modify: `PROJECT_STATUS.md`
- Modify: `CLAUDE.md`

- [ ] **Step 1: PROJECT_STATUS — neue v1.17.0-Sub-Task-Tabelle prepend**

In `PROJECT_STATUS.md` direkt nach der ueberschrift `## Aktueller Fortschritt (laufend)` und der ersten Stand-Zeile, und VOR `### Wo wir aufgehoert haben` einfuegen:

```markdown

### v1.17.0 — Artikel-Sync-Erweiterung (UNION + Meldebestand + Full-Update)

Hintergrund: Bisheriger Sage-Artikel-Sync lief nur fuer Artikel, die als Ressource in einer Stueckliste auftauchen, und uebernahm nur ArticleGroup bei Updates. Ab v1.17.0 liest der Sync auch aktive Bestellartikel (`IstBestellartikel = -1 AND Aktiv = -1`), uebernimmt den Meldebestand aus `KHKArtikelvarianten` und macht bei bestehenden Artikeln ein Full-Update aller 4 Sage-Felder.

| # | Sub-Task | Status |
|---|---------|--------|
| 0 | Pre-flight + Baseline-Build | ✅ erledigt |
| 1 | InternalsVisibleTo im Service-csproj | ✅ erledigt |
| 2 | SageImportHelpers.ParseReorderLevel + 8 Theory-Tests | ✅ erledigt |
| 3 | SageImportService.SyncArticlesAsync auf UNION + Full-Update umgestellt | ✅ erledigt |
| 4 | SQL/AgentJobs/02_Import_Artikel.sql DEPRECATED-Header | ✅ erledigt |
| 5 | AppVersion-Bump v1.17.0 (Web + Service) + Changelog-Card | ✅ erledigt |
| 6 | TESTSZENARIEN Kapitel 31 (6 manuelle Szenarien) | ✅ erledigt |
| 7 | CLAUDE.md Fallstrick fuer Sage VB6-Booleans | ✅ erledigt |
| 8 | Final-Check: Build + Tests + Code-Review | ⏳ offen |
| 9 | Merge feature/article-sync-erweiterung in main + Worktree-Cleanup | ⏳ offen |

```

(Hinweis: Wenn das Skript auf die markdown-Header trifft, achte darauf dass die Zeile danach mit `### Wo wir aufgehoert haben` weiter geht.)

- [ ] **Step 2: CLAUDE.md — neuer Fallstrick**

In `CLAUDE.md` im Abschnitt `## Bekannte Fallstricke` am Ende (nach dem letzten `- **...**:` Eintrag, vor `## Standard-Daten (Neuinstallation)`) einfuegen:

```markdown
- **Sage VB6-Booleans = BIT mit -1 fuer TRUE**: Sage-Tabellen (KHKArtikel, KHKArtikelvarianten, etc.) speichern Boolean-Spalten als BIT, aber mit `-1` fuer TRUE statt `1` (VB6-Legacy). Filter immer als `IstBestellartikel = -1 AND Aktiv = -1` formulieren, nicht `= 1` oder `= TRUE`. Bisher bekannte Spalten: `KHKArtikel.IstBestellartikel`, `KHKArtikel.Aktiv`.
```

- [ ] **Step 3: Commit**

```bash
git add PROJECT_STATUS.md CLAUDE.md
git commit -m "docs: PROJECT_STATUS v1.17.0 + CLAUDE.md Sage-VB6-Boolean-Fallstrick

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: Final-Check Build + Tests

**Files:** keine Aenderungen

- [ ] **Step 1: Vollstaendiger Solution-Build**

```bash
dotnet build IdealAkeWms.sln
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 2: Vollstaendige Test-Suite**

```bash
dotnet test IdealAkeWms.sln --no-build
```

Expected:
- Alle Web-Tests gruen
- Alle Service-Tests gruen + 8 neue ParseReorderLevel-Tests
- Insgesamt mind. 689 Tests passing (681 vorher + 8 neu) bzw. ein hoeherer Wert

- [ ] **Step 3: Versions-Sanity**

```bash
grep "1.17.0" IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs
grep "v1.17.0" IdealAkeWms/Views/Help/Changelog.cshtml | head -2
grep "v1.17.0" PROJECT_STATUS.md | head -2
```

Expected: Alle 4 Dateien erwaehnen `1.17.0` / `v1.17.0`.

- [ ] **Step 4: Worktree-Status**

```bash
git status
```

Expected: `nothing to commit, working tree clean`.

```bash
git log --oneline -10
```

Expected: 7 neue Commits seit `1108c0a docs(spec): ...`.

---

## Task 9: Merge in main + Worktree-Cleanup

**Files:** keine Code-Aenderungen — git-Operationen

- [ ] **Step 1: Auf main wechseln**

```bash
cd C:/Git/IDEAL-AKE-WMS
git checkout main
git pull origin main
```

Expected: main ist aktuell.

- [ ] **Step 2: Branch mergen**

```bash
git merge --no-ff feature/article-sync-erweiterung -m "merge feature/article-sync-erweiterung into main (v1.17.0)"
```

Expected: Merge-Commit erstellt.

- [ ] **Step 3: Build + Tests auf main**

```bash
dotnet build IdealAkeWms.sln && dotnet test IdealAkeWms.sln --no-build
```

Expected: clean build, alle Tests gruen.

- [ ] **Step 4: Push**

```bash
git push origin main
```

Expected: push succeeded.

- [ ] **Step 5: Branch + Worktree aufraeumen**

```bash
git worktree remove .claude/worktrees/article-sync-erweiterung
git branch -d feature/article-sync-erweiterung
git worktree list
```

Expected: `feature/article-sync-erweiterung` ist weg, Worktree-Liste enthaelt es nicht mehr.

Falls `worktree remove` mit Windows-File-Lock scheitert: manueller Fallback mit PowerShell `Remove-Item -Recurse -Force` nach Build-Server-Shutdown:

```powershell
dotnet build-server shutdown
Remove-Item -Recurse -Force C:\Git\IDEAL-AKE-WMS\.claude\worktrees\article-sync-erweiterung
```

---

## Final-Review-Subagent (nach Task 9)

Nach Abschluss aller Tasks einen separaten Code-Reviewer-Subagent dispatchen mit der diff-Range `29befc2..HEAD` (alle v1.17.0-Commits auf main inklusive Merge-Commit). Pruefkriterien:

1. Decken alle Commits die Spec-Kapitel ab? (Section 11 der Spec listet 6 Test-Szenarien — TESTSZENARIEN.md hat alle 6?)
2. SyncArticlesAsync: try/catch unveraendert, Connection-String-Check innerhalb des try-Blocks (CLAUDE.md-Konvention)?
3. ParseReorderLevel als `internal static` + InternalsVisibleTo gesetzt?
4. AgentJob-Skript: nur Header geaendert, Body unveraendert?
5. AppVersion synchron in Web + Service (beide 1.17.0)?
6. Changelog-Card auf v1.16.0 NICHT geaendert, nur v1.17.0 prependet?
7. CLAUDE.md: Fallstrick am korrekten Ort (vor `## Standard-Daten`)?
8. SyncResult-Counts-Keys deutschsprachig (`gelesen`, `neu`, `aktualisiert`) — passt zur v1.15.0-Konvention?
