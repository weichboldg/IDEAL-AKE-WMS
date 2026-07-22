# Artikel-Sync-Erweiterung — Design

**Datum:** 2026-05-28
**Status:** Draft
**Branch:** `feature/article-sync-erweiterung`
**Worktree:** `.claude/worktrees/article-sync-erweiterung`
**Version-Ziel:** v1.17.0

---

## 1. Ziel

`SageImportService.SyncArticlesAsync` synchronisiert deutlich mehr Sage-Artikel und uebernimmt den Meldebestand aus `KHKArtikelvarianten`. Bisher: nur Artikel, die in `KHKPpsRessourcenPositionen` als Ressource auftauchen (BOM-Komponenten). Neu zusaetzlich: alle Artikel mit `IstBestellartikel = -1 AND Aktiv = -1` (aktive Bestellartikel).

Begruendung: Das WMS soll auch Bestellartikel kennen, die noch nie in einer Stueckliste auftauchen — Voraussetzung fuer korrekte Bestand-Aggregation, Lagerbestellungen und Meldebestand-Ampeln.

## 2. Scope

**In-Scope:**

- Sage-SQL erweitern (UNION + KHKArtikelvarianten-LEFT-JOIN + GROUP-BY-Dedup)
- `SageImportService.SyncArticlesAsync` von INSERT-only-mit-ArticleGroup-Patch auf Full-Update-UPSERT umstellen
- ReorderLevel-Parsing (nvarchar(20) -> decimal?, InvariantCulture, 0 = NULL)
- `ParseReorderLevel`-Helper als `internal static`, Unit-Tests in `SageImportServiceTests`
- `SQL/AgentJobs/02_Import_Artikel.sql` mit Deprecation-Header
- Doku (Changelog, PROJECT_STATUS, TESTSZENARIEN Kapitel 31, CLAUDE.md)
- Version-Bump v1.17.0 (Web + Service)

**Out-of-Scope:**

- DB-Schema-Aenderung (Article.ReorderLevel ist bereits decimal?)
- Article-CRUD-UI-Aenderungen (Meldebestand-Spalte existiert bereits)
- Sage-Connection-String-Aenderungen
- `SQL/00_FreshInstall.sql`-Anpassung (keine Schema-Aenderung)

## 3. Entscheidungen aus Brainstorming

| Frage | Entscheidung |
|-------|--------------|
| Update-Strategie | **Full-Update aller Felder** — Description, Unit, ArticleGroup, ReorderLevel. Sage ist Master. |
| ReorderLevel-Parsing | **InvariantCulture, 0 -> NULL**. NULL/Empty -> NULL. Parse-Fehler -> NULL (still). |
| UNION-Dedup | **GROUP BY ArticleNumber, MAX() je Spalte** auf Sage-Seite. Deterministisch, skalierbar. |
| AgentJob-Skript | **Deprecation-Header** oben drueber. Body unveraendert. Skript bleibt fuer Failover. |
| Workflow | **Eigener Worktree** `.claude/worktrees/article-sync-erweiterung`, Branch `feature/article-sync-erweiterung` |

## 4. Architektur

Bestehendes Pattern bleibt: Service liest aus Sage via `SqlConnection`, schreibt pro Datensatz mit `MERGE`-aehnlichem IF EXISTS/UPDATE/ELSE INSERT in WMS. SyncLog-Lifecycle (`BeginRunAsync` -> `FinishSuccessAsync` / `FinishFailedAsync`) bleibt unveraendert.

```
Sage-DB
  └─> SqlConnection (SageConnection)
      └─> SELECT (CTE mit UNION + GROUP BY)
              │
              ▼
   IDEALAKEWMSService.SyncArticlesAsync
              │
       ParseReorderLevel(string?) -> decimal?
              │
              ▼
   SqlConnection (DefaultConnection)
              │
       IF EXISTS UPDATE ELSE INSERT (pro Datensatz)
              │
              ▼
       SyncResult { New, Updated, Errors }
              │
              ▼
       ISyncRun.FinishSuccessAsync(counts)
```

## 5. Sage-SQL (komplett)

```sql
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
GROUP BY ArticleNumber;
```

**Begruendung GROUP BY + MAX:** Da beide UNION-Teile aus denselben Quelltabellen (`KHKArtikel`/`KHKArtikelvarianten`) lesen, sollten Description/Unit/ArticleGroup/ReorderLevel pro `ArticleNumber` ueblicherweise identisch sein. `MAX()` bricht den Tie deterministisch und vermeidet Doppel-Inserts. `WHERE ArticleNumber IS NOT NULL AND != ''` ist defensiv, weil der zweite UNION-Teil keine eigene ArticleNumber-Filterung hat.

## 6. ReorderLevel-Parsing

```csharp
internal static class SageImportHelpers
{
    internal static decimal? ParseReorderLevel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)) return null;
        return value == 0m ? null : value;
    }
}
```

Datei: `IDEALAKEWMSService/Services/SageImportHelpers.cs`. `internal` + `InternalsVisibleTo("IDEALAKEWMSService.Tests")` (im Service-`csproj` ergaenzen falls fehlt).

**Verhalten:**

| Eingabe | Ergebnis | Begruendung |
|---------|----------|-------------|
| `null` | `null` | keine Daten |
| `""` | `null` | keine Daten |
| `"   "` | `null` | keine Daten |
| `"0"` | `null` | 0 = "kein Meldebestand gepflegt" (User-Vorgabe) |
| `"0.0000"` | `null` | wie oben |
| `"10.5"` | `10.5m` | normaler Fall |
| `"abc"` | `null` | Parse-Fehler still abfangen |
| `"-5"` | `-5m` | Parser tolerant; Sage liefert das nicht, aber kein Grund kuenstlich zu blockieren |

## 7. Write-Phase (WMS-UPSERT)

```sql
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
```

**Sentinel `-1` fuer ReorderLevel-Diff**: `decimal?` mit `IS NULL`-Vergleich macht das SQL haesslicher; `-1` ist ein Wert, den Sage nie liefert (Meldebestaende sind ≥ 0 — siehe auch User-Bestaetigung: 0 = kein Meldebestand).

## 8. SyncResult-Counts

Bisher:
```csharp
new Dictionary<string, int> { ["neu"] = inserted }
```

Neu (Vokabular wie in CLAUDE.md festgelegt: deutschsprachig — `"neu"`, `"aktualisiert"`, `"uebersprungen"`, `"fehler"`):

```csharp
new Dictionary<string, int>
{
    ["gelesen"]      = sageArticles.Count,
    ["neu"]          = inserted,
    ["aktualisiert"] = updated,
}
```

DryRun gibt `["gelesen"=N, "neu"=0, "aktualisiert"=0]` + Suffix `[DryRun]`.

## 9. AgentJob-Skript Deprecation

`SQL/AgentJobs/02_Import_Artikel.sql` bekommt oben drueber:

```sql
-- =============================================
-- ⚠️ DEPRECATED seit v1.17.0
-- Produktiver Sync laueft im IDEALAKEWMSService
-- (Sync:ArticlesEnabled = true). Dieses Skript ist nur
-- noch fuer manuelle Failover-Laeufe gedacht und nicht
-- mehr feature-vollstaendig:
--   - kein UNION mit IstBestellartikel/Aktiv-Filter
--   - keine Meldebestand-Uebernahme aus KHKArtikelvarianten
--   - kein Update bestehender Artikel
-- Bei Aenderungen am Service-Sync bitte hier NICHT nachziehen.
-- =============================================
```

Body unveraendert.

## 10. Error-Handling

| Szenario | Verhalten |
|----------|-----------|
| Sage-Connection-String fehlt | `InvalidOperationException` -> `FinishFailedAsync` (wie bestehend) |
| WMS-Connection-String fehlt | `InvalidOperationException` -> `FinishFailedAsync` |
| Sage-SQL-Exception | Exception propagiert -> catch -> `LogErrorAsync` + `FinishFailedAsync` + rethrow |
| Einzelner UPSERT scheitert | komplette `SyncArticlesAsync` bricht ab (Sage-Master-Konsistenz wichtiger als Teil-Sync) |
| `ReorderLevel` nicht parsebar | Still als `null` (kein Log-Spam, Sage-Datenqualitaet wechselhaft) |
| `ArticleNumber` leer/NULL | bereits in Sage-SQL ausgefiltert (`WHERE ArticleNumber IS NOT NULL AND != ''`) |

## 11. Testing

Bestehende Tests (`SageImportServiceTests`):
- `SyncArticlesAsync_writes_lifecycle_via_failure_path` — bleibt unveraendert (Failure-Path mit fehlender Connection-String)

Neu hinzu in derselben Datei:

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
        result.Should().Be(decimal.Parse(expected, CultureInfo.InvariantCulture));
}
```

**Decimal-Parameter** bei `[InlineData]` ist im Test schwierig (Compiler-Limit) — deshalb String + im Body parsen.

**Integrationstest fuer Sage-SQL/WMS-UPSERT:** nicht moeglich, weil der Service raw ADO.NET ohne EF-InMemory verwendet. Wird via TESTSZENARIEN Kapitel 31 manuell abgenommen.

**Manuelle Testszenarien (TESTSZENARIEN.md Kapitel 31):**

1. **Neuer Bestellartikel ohne BOM-Beteiligung kommt an**
   - Vorbedingung: Artikel `TEST-9999` in Sage `KHKArtikel` mit `IstBestellartikel = -1, Aktiv = -1`, nicht in `KHKPpsRessourcenPositionen`
   - Aktion: Service-Sync starten
   - Erwartung: Artikel in `Articles`-Tabelle eingefuegt, `CreatedBy = 'IDEALAKEWMSService'`

2. **Meldebestand-Uebernahme**
   - Vorbedingung: Artikel `TEST-9999` mit `Meldebestand = 25.0000` in `KHKArtikelvarianten`
   - Aktion: Service-Sync starten
   - Erwartung: `Articles.ReorderLevel = 25.0000`

3. **Meldebestand 0 wird zu NULL**
   - Vorbedingung: Artikel `TEST-9998` mit `Meldebestand = 0` in `KHKArtikelvarianten`
   - Aktion: Service-Sync starten
   - Erwartung: `Articles.ReorderLevel IS NULL`

4. **Full-Update bei bestehenden Artikeln**
   - Vorbedingung: Artikel `TEST-9999` existiert in WMS mit `Description = "alt"`, in Sage geaendert auf `Description = "neu"`
   - Aktion: Service-Sync starten
   - Erwartung: `Articles.Description = "neu"`, `ModifiedAt` gesetzt, `ModifiedBy = 'IDEALAKEWMSService'`

5. **Idempotenz: Zweiter Lauf ohne Aenderungen schreibt nichts**
   - Vorbedingung: Sync wurde gerade gelaufen
   - Aktion: Sync sofort nochmal starten
   - Erwartung: SyncLog `aktualisiert = 0`, `neu = 0`

6. **DryRun-Modus**
   - Aktion: `WorkerSettings:SyncDryRun = true`, Service-Sync starten
   - Erwartung: SyncLog-Eintrag mit Suffix `[DryRun]`, keine WMS-Aenderungen

## 12. Doku-Aenderungen

- `IdealAkeWms/AppVersion.cs` -> `Version = "1.17.0", Date = "2026-05-28"`
- `IDEALAKEWMSService/AppVersion.cs` -> identisch
- `IdealAkeWms/Views/Help/Changelog.cshtml` -> neuer v1.17.0-Card mit 4 Bulletpoints (UNION, Meldebestand, Full-Update, AgentJob deprecated)
- `PROJECT_STATUS.md` -> neue Sub-Task-Tabelle v1.17.0
- `docs/TESTSZENARIEN.md` -> neues Kapitel 31 mit 6 Szenarien (siehe oben)
- `CLAUDE.md` -> neuer Fallstrick: `Sage Boolean-Spalten = BIT mit -1 fuer TRUE (VB6-Legacy, NICHT 1)`

## 13. Reihenfolge der Tasks (grob, Detailplanung in writing-plans)

1. Pre-Flight: Build + Tests gruen auf neuem Branch
2. `SageImportHelpers.ParseReorderLevel` + Unit-Tests (TDD)
3. `SageImportService.SyncArticlesAsync` umstellen
4. AgentJob-Skript Deprecation-Header
5. Doku (Changelog, PROJECT_STATUS, TESTSZENARIEN, CLAUDE.md, Version-Bump)
6. Manuelle Tests gegen DEV-Sage (vor Merge)
7. Merge in main + Worktree-Cleanup

## 14. Offen / Risiken

- **`InternalsVisibleTo` im Service-Projekt**: pruefen ob schon gesetzt. Falls nicht: in `IDEALAKEWMSService.csproj` ergaenzen.
- **Sage-Performance**: Neue UNION-Query mit zwei LEFT-JOINs auf `KHKArtikelvarianten` kann groesseren Result-Set liefern. Bei 50k+ Artikeln evtl. CommandTimeout > 120s noetig. Beobachten beim Erst-Lauf.
- **Existierende Test-Artikel**: WMS-Artikel mit `ReorderLevel`-Werten, die in Sage nicht (mehr) gepflegt sind, werden auf `NULL` ueberschrieben. Akzeptiert (Sage = Master).
