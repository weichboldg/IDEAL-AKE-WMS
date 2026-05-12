# Sage Lagerplatz-Sync (Phase 1) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Lagerplatz-Stammdaten aus Sage (`KHKLagerorte` + `KHKLagerplaetze`) periodisch ins WMS spiegeln. Sage ist Master fuer synchronisierte Records (Code/Zone/Description/IsActive); manuelle Lagerplaetze bleiben unberuehrt; Konflikte werden in einer neuen `SyncLog`-Tabelle protokolliert.

**Architecture:** Worker-Service `LagerplatzSyncService` im `IDEALAKEWMSService` liest Sage via `ISageLagerplatzReader` (raw SQL, mockbar in Tests) und schreibt WMS via `ApplicationDbContext`. Neue Felder `Source` (Manual/Sage) und `IsActive` auf `StorageLocation`; neue Entity `SyncLog` (service-uebergreifend). Edit-Maske im Web-Projekt sperrt Sage-Felder client+server-seitig.

**Tech Stack:** .NET 10, EF Core 10 (SQL Server), xUnit + FluentAssertions + Moq fuer Tests, Serilog, ASP.NET Core MVC. Bestehende Pattern: SyncWorker mit ServiceSetting-Toggles, repository pattern.

**Branch:** `feature/sage-lagerplatz-sync` (bereits angelegt, Spec committed als `29cb0f9`).

**Spec:** [docs/superpowers/specs/2026-05-05-sage-lagerplatz-sync-design.md](../specs/2026-05-05-sage-lagerplatz-sync-design.md)

**Commit-Konvention:** `feat(lagerplatz): ...` / `test(lagerplatz): ...` / `fix(lagerplatz): ...` / `docs: ...`. Co-Authored-By trailer wie in recent commits.

---

## Phase 1 — Datenmodell und Schema

### Task 1: StorageLocation um Source und IsActive erweitern

**Files:**
- Modify: `IdealAkeWms/Models/StorageLocation.cs`
- Create: `IdealAkeWms/Models/StorageLocationSource.cs`
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs:153-169` (StorageLocation-Konfiguration)
- Create: `IdealAkeWms/Migrations/<timestamp>_AddStorageLocationSyncFields.cs` (per `dotnet ef`)
- Create: `SQL/55_AddStorageLocationSyncFields.sql`
- Modify: `SQL/00_FreshInstall.sql` (StorageLocations-Tabelle Section)

- [ ] **Step 1: Konstanten-Klasse anlegen**

```csharp
// IdealAkeWms/Models/StorageLocationSource.cs
namespace IdealAkeWms.Models;

public static class StorageLocationSource
{
    public const string Manual = "Manual";
    public const string Sage = "Sage";
}
```

- [ ] **Step 2: Properties auf StorageLocation hinzufuegen**

In `IdealAkeWms/Models/StorageLocation.cs`, nach `IsPickingTransport`, einfuegen:

```csharp
[StringLength(20)]
[Display(Name = "Quelle")]
public string Source { get; set; } = StorageLocationSource.Manual;

[Display(Name = "Aktiv")]
public bool IsActive { get; set; } = true;
```

- [ ] **Step 3: EF-Konfiguration in ApplicationDbContext erweitern**

In `IdealAkeWms/Data/ApplicationDbContext.cs`, im `modelBuilder.Entity<StorageLocation>`-Block (Zeile 154-169), vor `entity.HasIndex(e => e.Code).IsUnique();` einfuegen:

```csharp
entity.Property(e => e.Source).HasMaxLength(20).IsRequired().HasDefaultValue(StorageLocationSource.Manual);
entity.Property(e => e.IsActive).HasDefaultValue(true);
entity.HasIndex(e => e.IsActive);
entity.HasIndex(e => e.Source);
```

- [ ] **Step 4: EF-Migration generieren**

Run:
```pwsh
dotnet ef migrations add AddStorageLocationSyncFields --project IdealAkeWms
```

Expected: zwei neue Dateien unter `IdealAkeWms/Migrations/` (`*_AddStorageLocationSyncFields.cs` + `*.Designer.cs`). Migration enthaelt `AddColumn` fuer `Source` und `IsActive` mit Defaults.

- [ ] **Step 5: SQL/55-Skript erstellen**

```sql
-- SQL/55_AddStorageLocationSyncFields.sql
-- Phase: Sage Lagerplatz-Sync — Source + IsActive auf StorageLocations
-- Idempotent: COL_LENGTH-Guard pro Spalte.

IF COL_LENGTH('dbo.StorageLocations', 'Source') IS NULL
BEGIN
    ALTER TABLE dbo.StorageLocations
        ADD Source NVARCHAR(20) NOT NULL CONSTRAINT DF_StorageLocations_Source DEFAULT 'Manual';
END
GO

IF COL_LENGTH('dbo.StorageLocations', 'IsActive') IS NULL
BEGIN
    ALTER TABLE dbo.StorageLocations
        ADD IsActive BIT NOT NULL CONSTRAINT DF_StorageLocations_IsActive DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_StorageLocations_IsActive'
      AND object_id = OBJECT_ID('dbo.StorageLocations'))
BEGIN
    CREATE INDEX IX_StorageLocations_IsActive ON dbo.StorageLocations(IsActive);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_StorageLocations_Source'
      AND object_id = OBJECT_ID('dbo.StorageLocations'))
BEGIN
    CREATE INDEX IX_StorageLocations_Source ON dbo.StorageLocations(Source);
END
GO

-- EF-Migrationshistorie eintragen, sodass App den Schritt ueberspringt
-- HINWEIS: MigrationId aus dem generierten C#-Migrations-Dateinamen uebernehmen.
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId LIKE '%_AddStorageLocationSyncFields')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    SELECT '<TIMESTAMP>_AddStorageLocationSyncFields', '10.0.2';
END
GO
```

Den `<TIMESTAMP>`-Platzhalter durch den tatsaechlichen Timestamp aus dem in Step 4 generierten Migration-Dateinamen ersetzen.

- [ ] **Step 6: FreshInstall.sql aktualisieren**

In `SQL/00_FreshInstall.sql` die `StorageLocations`-Tabellen-DDL um die zwei neuen Spalten + zwei Indizes ergaenzen, analog zur Form in 55-Skript. MigrationId-Eintrag in der `__EFMigrationsHistory`-Insert-Liste am Ende der Datei ergaenzen.

- [ ] **Step 7: Build verifizieren**

Run:
```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

Expected: `0 Fehler`.

- [ ] **Step 8: Vorhandene Tests laufen lassen**

Run:
```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --nologo
```

Expected: alle 462+ Tests gruen.

- [ ] **Step 9: Commit**

```pwsh
git add IdealAkeWms/Models/StorageLocation.cs IdealAkeWms/Models/StorageLocationSource.cs IdealAkeWms/Data/ApplicationDbContext.cs IdealAkeWms/Migrations/ SQL/55_AddStorageLocationSyncFields.sql SQL/00_FreshInstall.sql
git commit -m "feat(lagerplatz): add Source + IsActive columns to StorageLocations" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: SyncLog Entity + Schema

**Files:**
- Create: `IdealAkeWms/Models/SyncLog.cs`
- Create: `IdealAkeWms/Models/SyncLogLevel.cs` (Konstanten)
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs` (DbSet + Konfiguration)
- Create: `IdealAkeWms/Migrations/<timestamp>_AddSyncLog.cs` (per `dotnet ef`)
- Create: `SQL/56_AddSyncLog.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: SyncLogLevel-Konstanten**

```csharp
// IdealAkeWms/Models/SyncLogLevel.cs
namespace IdealAkeWms.Models;

public static class SyncLogLevel
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Error = "Error";
}
```

- [ ] **Step 2: SyncLog-Entity**

```csharp
// IdealAkeWms/Models/SyncLog.cs
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class SyncLog
{
    public int Id { get; set; }

    [Display(Name = "Zeitpunkt")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [Required]
    [StringLength(50)]
    [Display(Name = "Service")]
    public string Service { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    [Display(Name = "Stufe")]
    public string Level { get; set; } = SyncLogLevel.Info;

    [Required]
    [StringLength(1000)]
    [Display(Name = "Meldung")]
    public string Message { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Referenz")]
    public string? Reference { get; set; }
}
```

- [ ] **Step 3: ApplicationDbContext erweitern**

In `IdealAkeWms/Data/ApplicationDbContext.cs`:
- Neben den bestehenden `DbSet<...>`-Properties:

```csharp
public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
```

- Im `OnModelCreating`, nach dem letzten existierenden `modelBuilder.Entity<...>`-Block:

```csharp
modelBuilder.Entity<SyncLog>(entity =>
{
    entity.ToTable("SyncLogs");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Service).HasMaxLength(50).IsRequired();
    entity.Property(e => e.Level).HasMaxLength(10).IsRequired();
    entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
    entity.Property(e => e.Reference).HasMaxLength(100);

    entity.HasIndex(e => e.Timestamp).IsDescending().HasDatabaseName("IX_SyncLogs_Timestamp_Desc");
    entity.HasIndex(e => new { e.Service, e.Level }).HasDatabaseName("IX_SyncLogs_Service_Level");
});
```

- [ ] **Step 4: EF-Migration generieren**

Run:
```pwsh
dotnet ef migrations add AddSyncLog --project IdealAkeWms
```

Expected: neue Dateien `*_AddSyncLog.cs` + `.Designer.cs`. Migration enthaelt `CreateTable("SyncLogs", ...)` plus zwei Indizes.

- [ ] **Step 5: SQL/56-Skript erstellen**

```sql
-- SQL/56_AddSyncLog.sql
-- Phase: Sage Lagerplatz-Sync — service-uebergreifendes Sync-Protokoll.

IF OBJECT_ID('dbo.SyncLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SyncLogs (
        Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SyncLogs PRIMARY KEY,
        Timestamp DATETIME2 NOT NULL CONSTRAINT DF_SyncLogs_Timestamp DEFAULT SYSDATETIME(),
        Service   NVARCHAR(50)  NOT NULL,
        Level     NVARCHAR(10)  NOT NULL,
        Message   NVARCHAR(1000) NOT NULL,
        Reference NVARCHAR(100)  NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_SyncLogs_Timestamp_Desc'
      AND object_id = OBJECT_ID('dbo.SyncLogs'))
BEGIN
    CREATE INDEX IX_SyncLogs_Timestamp_Desc ON dbo.SyncLogs([Timestamp] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_SyncLogs_Service_Level'
      AND object_id = OBJECT_ID('dbo.SyncLogs'))
BEGIN
    CREATE INDEX IX_SyncLogs_Service_Level ON dbo.SyncLogs([Service], [Level]);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId LIKE '%_AddSyncLog')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    SELECT '<TIMESTAMP>_AddSyncLog', '10.0.2';
END
GO
```

Timestamp wieder durch echten Wert ersetzen.

- [ ] **Step 6: FreshInstall.sql aktualisieren**

CreateTable + Indizes + MigrationsHistory-Eintrag analog ergaenzen.

- [ ] **Step 7: Build + bestehende Tests**

```pwsh
dotnet build --nologo
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --nologo
```

Expected: `0 Fehler`, alle Tests gruen.

- [ ] **Step 8: Commit**

```pwsh
git add IdealAkeWms/Models/SyncLog.cs IdealAkeWms/Models/SyncLogLevel.cs IdealAkeWms/Data/ApplicationDbContext.cs IdealAkeWms/Migrations/ SQL/56_AddSyncLog.sql SQL/00_FreshInstall.sql
git commit -m "feat(sync): add SyncLog table for service-wide sync diagnostics" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: SyncLogRepository (Web-Projekt) + DI + Test

**Files:**
- Create: `IdealAkeWms/Data/Repositories/ISyncLogRepository.cs`
- Create: `IdealAkeWms/Data/Repositories/SyncLogRepository.cs`
- Modify: `IdealAkeWms/Program.cs` (DI Registration)
- Modify: `IDEALAKEWMSService/Program.cs` (DI Registration)
- Create: `IdealAkeWms.Tests/Repositories/SyncLogRepositoryTests.cs`

- [ ] **Step 1: Test schreiben (failing)**

```csharp
// IdealAkeWms.Tests/Repositories/SyncLogRepositoryTests.cs
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Repositories;

public class SyncLogRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsEntry()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new SyncLogRepository(ctx);

        await repo.AddAsync(new SyncLog
        {
            Service = "Lagerplatz",
            Level = SyncLogLevel.Warning,
            Message = "Konflikt: ABC manuell",
            Reference = "ABC"
        });

        var all = await repo.GetRecentAsync(service: null, level: null, limit: 10);
        all.Should().ContainSingle();
        all[0].Service.Should().Be("Lagerplatz");
        all[0].Reference.Should().Be("ABC");
    }

    [Fact]
    public async Task GetRecentAsync_FiltersByServiceAndLevel_OrdersDesc()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new SyncLogRepository(ctx);

        await repo.AddAsync(new SyncLog { Service = "Lagerplatz", Level = SyncLogLevel.Info,    Message = "A", Timestamp = new DateTime(2026, 5, 1) });
        await repo.AddAsync(new SyncLog { Service = "Lagerplatz", Level = SyncLogLevel.Warning, Message = "B", Timestamp = new DateTime(2026, 5, 3) });
        await repo.AddAsync(new SyncLog { Service = "OseonTracking", Level = SyncLogLevel.Warning, Message = "C", Timestamp = new DateTime(2026, 5, 2) });

        var lagerplatzWarnings = await repo.GetRecentAsync(service: "Lagerplatz", level: SyncLogLevel.Warning, limit: 10);
        lagerplatzWarnings.Should().HaveCount(1);
        lagerplatzWarnings[0].Message.Should().Be("B");

        var allDesc = await repo.GetRecentAsync(service: null, level: null, limit: 10);
        allDesc.Select(x => x.Message).Should().ContainInOrder("B", "C", "A");
    }
}
```

- [ ] **Step 2: Test ausfuehren (FAIL erwartet — Repository existiert nicht)**

Run:
```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~SyncLogRepositoryTests" --nologo
```

Expected: Compile-Error oder TypeNotFound.

- [ ] **Step 3: Interface schreiben**

```csharp
// IdealAkeWms/Data/Repositories/ISyncLogRepository.cs
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface ISyncLogRepository
{
    Task AddAsync(SyncLog entry);
    Task<List<SyncLog>> GetRecentAsync(string? service, string? level, int limit);
}
```

- [ ] **Step 4: Implementation**

```csharp
// IdealAkeWms/Data/Repositories/SyncLogRepository.cs
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class SyncLogRepository : ISyncLogRepository
{
    private readonly ApplicationDbContext _context;

    public SyncLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SyncLog entry)
    {
        _context.SyncLogs.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task<List<SyncLog>> GetRecentAsync(string? service, string? level, int limit)
    {
        IQueryable<SyncLog> q = _context.SyncLogs;
        if (!string.IsNullOrWhiteSpace(service))
            q = q.Where(x => x.Service == service);
        if (!string.IsNullOrWhiteSpace(level))
            q = q.Where(x => x.Level == level);
        return await q.OrderByDescending(x => x.Timestamp).Take(limit).ToListAsync();
    }
}
```

- [ ] **Step 5: DI Registration im Web-Projekt**

In `IdealAkeWms/Program.cs`, in der Repository-Registrierungs-Sektion (vor `app.Run()`/`builder.Build()`), neben den anderen `AddScoped` zeilen einfuegen:

```csharp
builder.Services.AddScoped<ISyncLogRepository, SyncLogRepository>();
```

- [ ] **Step 6: DI Registration im Service-Projekt**

In `IDEALAKEWMSService/Program.cs`, im Repositories-Block, einfuegen:

```csharp
builder.Services.AddScoped<ISyncLogRepository, SyncLogRepository>();
```

- [ ] **Step 7: Tests laufen lassen — sollen jetzt PASS**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~SyncLogRepositoryTests" --nologo
```

Expected: 2/2 PASS.

- [ ] **Step 8: Build + alle Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: alles gruen.

- [ ] **Step 9: Commit**

```pwsh
git add IdealAkeWms/Data/Repositories/ISyncLogRepository.cs IdealAkeWms/Data/Repositories/SyncLogRepository.cs IdealAkeWms/Program.cs IDEALAKEWMSService/Program.cs IdealAkeWms.Tests/Repositories/SyncLogRepositoryTests.cs
git commit -m "feat(sync): add SyncLogRepository with filter + recent-N query" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: SageLagerplatzReader (Interface + DTO + Real Impl + DI)

**Files:**
- Create: `IDEALAKEWMSService/Services/ISageLagerplatzReader.cs`
- Create: `IDEALAKEWMSService/Services/SageLagerplatzReader.cs`
- Modify: `IDEALAKEWMSService/Program.cs`

Hinweis: Der Reader laesst sich nicht sinnvoll mit InMemory-DB testen (raw SQL gegen externe Sage-DB). Test-Coverage erfolgt indirekt im Sync-Service-Test ueber einen `FakeSageLagerplatzReader`. Manuelle Verifikation ist die einzige Pruefung der echten SQL-Implementierung.

- [ ] **Step 1: DTO + Interface schreiben**

```csharp
// IDEALAKEWMSService/Services/ISageLagerplatzReader.cs
namespace IDEALAKEWMSService.Services;

/// <summary>DTO from SAGE — null-able strings spiegeln Sage-Realitaet wider.</summary>
public record SageLagerplatzDto(string? Lagerkennung, string? Kurzbezeichnung, string? Platzbezeichnung);

public interface ISageLagerplatzReader
{
    Task<List<SageLagerplatzDto>> GetAllActiveAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Real Implementation**

```csharp
// IDEALAKEWMSService/Services/SageLagerplatzReader.cs
using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

public class SageLagerplatzReader : ISageLagerplatzReader
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SageLagerplatzReader> _logger;

    public SageLagerplatzReader(IConfiguration configuration, ILogger<SageLagerplatzReader> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<SageLagerplatzDto>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var sageConnection = _configuration.GetConnectionString("SageConnection")
            ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");

        // ANNAHME: KHKLagerorte hat eine Mandant-Spalte (analog Artikel-Sync).
        // Falls Sage-Schema das nicht hat, "AND lo.Mandant = 1" entfernen.
        const string sql = """
            SELECT lo.Lagerkennung, lp.Kurzbezeichnung, lp.Platzbezeichnung
            FROM KHKLagerorte lo
            LEFT JOIN KHKLagerplaetze lp ON lo.Lagerkennung = lp.Lagerkennung
            WHERE lo.Mandant = 1
              AND lo.Aktiv = -1
            """;

        var result = new List<SageLagerplatzDto>();

        await using var conn = new SqlConnection(sageConnection);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            result.Add(new SageLagerplatzDto(
                Lagerkennung: reader.IsDBNull(0) ? null : reader.GetString(0),
                Kurzbezeichnung: reader.IsDBNull(1) ? null : reader.GetString(1),
                Platzbezeichnung: reader.IsDBNull(2) ? null : reader.GetString(2)
            ));
        }

        _logger.LogInformation("Sage liefert {Count} aktive Lagerplaetze.", result.Count);
        return result;
    }
}
```

- [ ] **Step 3: DI Registration**

In `IDEALAKEWMSService/Program.cs`, im Services-Block, einfuegen:

```csharp
builder.Services.AddScoped<ISageLagerplatzReader, SageLagerplatzReader>();
```

- [ ] **Step 4: Build verifizieren**

```pwsh
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj --nologo
```

Expected: `0 Fehler`.

- [ ] **Step 5: Commit**

```pwsh
git add IDEALAKEWMSService/Services/ISageLagerplatzReader.cs IDEALAKEWMSService/Services/SageLagerplatzReader.cs IDEALAKEWMSService/Program.cs
git commit -m "feat(lagerplatz): add SageLagerplatzReader with raw SQL against KHKLagerorte/KHKLagerplaetze" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 2 — Sync-Service (TDD)

### Task 5: LagerplatzSyncService Skeleton + Test 1 (Insert)

**Files:**
- Create: `IDEALAKEWMSService/Services/ILagerplatzSyncService.cs`
- Create: `IDEALAKEWMSService/Services/LagerplatzSyncService.cs`
- Create: `IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs`
- Create: `IDEALAKEWMSService.Tests/Helpers/FakeSageLagerplatzReader.cs`

- [ ] **Step 1: Result-Record + Interface schreiben**

```csharp
// IDEALAKEWMSService/Services/ILagerplatzSyncService.cs
namespace IDEALAKEWMSService.Services;

public record LagerplatzSyncResult(int Inserted, int Updated, int Conflicts, int Deactivated, int Skipped, int Errors);

public interface ILagerplatzSyncService
{
    Task<LagerplatzSyncResult> RunAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: FakeSageLagerplatzReader**

```csharp
// IDEALAKEWMSService.Tests/Helpers/FakeSageLagerplatzReader.cs
using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Tests.Helpers;

public class FakeSageLagerplatzReader : ISageLagerplatzReader
{
    public List<SageLagerplatzDto> Records { get; set; } = new();
    public Func<List<SageLagerplatzDto>>? RecordsFactory { get; set; }
    public Exception? ThrowOnRead { get; set; }

    public Task<List<SageLagerplatzDto>> GetAllActiveAsync(CancellationToken ct = default)
    {
        if (ThrowOnRead != null)
            throw ThrowOnRead;
        return Task.FromResult(RecordsFactory?.Invoke() ?? Records);
    }
}
```

- [ ] **Step 3: Erster Test — Insert in leere DB**

```csharp
// IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;            // TestDbContextFactory liegt im Web-Test-Projekt
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace IDEALAKEWMSService.Tests.Services;

public class LagerplatzSyncServiceTests
{
    private static (LagerplatzSyncService service, FakeSageLagerplatzReader reader, IdealAkeWms.Data.ApplicationDbContext ctx, SyncLogRepository syncLogs)
        Build()
    {
        var ctx = TestDbContextFactory.Create();
        var reader = new FakeSageLagerplatzReader();
        var syncLogs = new SyncLogRepository(ctx);
        var service = new LagerplatzSyncService(ctx, reader, syncLogs, NullLogger<LagerplatzSyncService>.Instance);
        return (service, reader, ctx, syncLogs);
    }

    [Fact]
    public async Task Run_EmptyDb_ThreeSagePlaetze_InsertsThree()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        reader.Records = new()
        {
            new("HALLE-1", "A-01-01", "Regal A1"),
            new("HALLE-1", "A-01-02", "Regal A2"),
            new("HALLE-2", "B-01-01", "Lager Aussen"),
        };

        var result = await svc.RunAsync();

        result.Inserted.Should().Be(3);
        result.Updated.Should().Be(0);
        result.Conflicts.Should().Be(0);

        var stored = ctx.StorageLocations.OrderBy(s => s.Code).ToList();
        stored.Should().HaveCount(3);
        stored[0].Code.Should().Be("A-01-01");
        stored[0].Zone.Should().Be("HALLE-1");
        stored[0].Description.Should().Be("Regal A1");
        stored[0].Source.Should().Be(StorageLocationSource.Sage);
        stored[0].IsActive.Should().BeTrue();
        stored[0].BarcodeValue.Should().Be("A-01-01");
        stored[0].Capacity.Should().BeNull();
        stored[0].IsPickingTransport.Should().BeFalse();

        var summary = (await syncLogs.GetRecentAsync("Lagerplatz", null, 10)).FirstOrDefault();
        summary.Should().NotBeNull();
        summary!.Level.Should().Be(SyncLogLevel.Info);
        summary.Message.Should().Contain("3 neu");
    }
}
```

- [ ] **Step 4: Test laufen lassen — soll Compile-Error werfen weil LagerplatzSyncService noch fehlt**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

Expected: Compile-Error.

- [ ] **Step 5: Minimal-Implementierung — nur Insert-Pfad**

```csharp
// IDEALAKEWMSService/Services/LagerplatzSyncService.cs
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class LagerplatzSyncService : ILagerplatzSyncService
{
    private const string ServiceName = "Lagerplatz";
    private const int MaxCodeLength = 50;
    private const int MaxDescriptionLength = 200;
    private const string SyncUser = "system:sync";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageLagerplatzReader _reader;
    private readonly ISyncLogRepository _syncLogs;
    private readonly ILogger<LagerplatzSyncService> _logger;

    public LagerplatzSyncService(
        ApplicationDbContext ctx,
        ISageLagerplatzReader reader,
        ISyncLogRepository syncLogs,
        ILogger<LagerplatzSyncService> logger)
    {
        _ctx = ctx;
        _reader = reader;
        _syncLogs = syncLogs;
        _logger = logger;
    }

    public async Task<LagerplatzSyncResult> RunAsync(CancellationToken ct = default)
    {
        int inserted = 0, updated = 0, conflicts = 0, deactivated = 0, skipped = 0, errors = 0;

        List<SageLagerplatzDto> sageRecords;
        try
        {
            sageRecords = await _reader.GetAllActiveAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Sage-Connection fehlgeschlagen.");
            await _syncLogs.AddAsync(new SyncLog
            {
                Service = ServiceName,
                Level = SyncLogLevel.Error,
                Message = $"Sage-Connection fehlgeschlagen: {ex.Message}"
            });
            return new LagerplatzSyncResult(0, 0, 0, 0, 0, 1);
        }

        var existing = await _ctx.StorageLocations.ToListAsync(ct);
        var byCode = existing.ToDictionary(x => x.Code, x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var dto in sageRecords)
        {
            if (string.IsNullOrWhiteSpace(dto.Kurzbezeichnung))
            {
                skipped++;
                continue;
            }

            var code = dto.Kurzbezeichnung.Trim();
            if (!byCode.ContainsKey(code))
            {
                _ctx.StorageLocations.Add(new StorageLocation
                {
                    Code = code,
                    Zone = dto.Lagerkennung,
                    Description = dto.Platzbezeichnung,
                    BarcodeValue = code,
                    Source = StorageLocationSource.Sage,
                    IsActive = true,
                    Capacity = null,
                    IsPickingTransport = false,
                    CreatedAt = DateTime.Now,
                    CreatedBy = SyncUser,
                    CreatedByWindows = Environment.MachineName
                });
                inserted++;
            }
        }

        await _ctx.SaveChangesAsync(ct);

        await _syncLogs.AddAsync(new SyncLog
        {
            Service = ServiceName,
            Level = SyncLogLevel.Info,
            Message = $"Sync OK: {inserted} neu, {updated} aktualisiert, {conflicts} Konflikte, {deactivated} deaktiviert."
        });

        return new LagerplatzSyncResult(inserted, updated, conflicts, deactivated, skipped, errors);
    }
}
```

- [ ] **Step 6: Test laufen — soll PASS**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

Expected: 1/1 PASS.

- [ ] **Step 7: Commit**

```pwsh
git add IDEALAKEWMSService/Services/ILagerplatzSyncService.cs IDEALAKEWMSService/Services/LagerplatzSyncService.cs IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs IDEALAKEWMSService.Tests/Helpers/FakeSageLagerplatzReader.cs
git commit -m "feat(lagerplatz): LagerplatzSyncService skeleton with insert-path + test" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Test 2 + 3 (Update bei Diff / No-Op)

**Files:**
- Modify: `IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs`
- Modify: `IDEALAKEWMSService/Services/LagerplatzSyncService.cs`

- [ ] **Step 1: Tests anhaengen**

```csharp
[Fact]
public async Task Run_ExistingSageRecord_DescriptionDiff_UpdatesAndStampsModified()
{
    var (svc, reader, ctx, _) = Build();
    ctx.StorageLocations.Add(new StorageLocation
    {
        Code = "A-01-01", Zone = "HALLE-1", Description = "Alte Bezeichnung",
        BarcodeValue = "A-01-01", Source = StorageLocationSource.Sage, IsActive = true,
        CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = SyncUser_For_Tests, CreatedByWindows = "x"
    });
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("HALLE-1", "A-01-01", "Neue Bezeichnung") };

    var result = await svc.RunAsync();

    result.Updated.Should().Be(1);
    result.Inserted.Should().Be(0);
    var sl = ctx.StorageLocations.Single();
    sl.Description.Should().Be("Neue Bezeichnung");
    sl.ModifiedAt.Should().NotBeNull();
    sl.ModifiedBy.Should().Be(SyncUser_For_Tests);
}

[Fact]
public async Task Run_ExistingSageRecord_NoDiff_DoesNotUpdate()
{
    var (svc, reader, ctx, _) = Build();
    var original = new StorageLocation
    {
        Code = "A-01-01", Zone = "HALLE-1", Description = "Regal A1",
        BarcodeValue = "A-01-01", Source = StorageLocationSource.Sage, IsActive = true,
        CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = SyncUser_For_Tests, CreatedByWindows = "x"
    };
    ctx.StorageLocations.Add(original);
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("HALLE-1", "A-01-01", "Regal A1") };

    var result = await svc.RunAsync();

    result.Updated.Should().Be(0);
    var sl = ctx.StorageLocations.Single();
    sl.ModifiedAt.Should().BeNull();
}
```

Plus oben in der Klasse als private const: `private const string SyncUser_For_Tests = "system:sync";`

- [ ] **Step 2: Tests laufen — Test 2 FAIL (kein Update-Pfad), Test 3 wird falsch passieren wenn Update-Pfad fehlt**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

Expected: 1 PASS (Insert), 1 FAIL (Update bei Diff), 1 PASS (No-Op zufaellig, weil noch kein Update implementiert ist).

- [ ] **Step 3: Service erweitern — Update-Branch mit Diff-Detection**

Im `LagerplatzSyncService.RunAsync`, in der `foreach`-Schleife, nach dem `if (!byCode.ContainsKey(code))`-Block einen `else`-Branch ergaenzen:

```csharp
else
{
    var existingLoc = byCode[code];
    if (existingLoc.Source == StorageLocationSource.Sage)
    {
        var newZone = dto.Lagerkennung;
        var newDescription = dto.Platzbezeichnung;
        var diff = existingLoc.Zone != newZone
                || existingLoc.Description != newDescription
                || existingLoc.BarcodeValue != code
                || !existingLoc.IsActive;

        if (diff)
        {
            existingLoc.Zone = newZone;
            existingLoc.Description = newDescription;
            existingLoc.BarcodeValue = code;
            existingLoc.IsActive = true;
            existingLoc.ModifiedAt = DateTime.Now;
            existingLoc.ModifiedBy = SyncUser;
            existingLoc.ModifiedByWindows = Environment.MachineName;
            updated++;
        }
    }
}
```

- [ ] **Step 4: Tests laufen — alle 3 PASS**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

Expected: 3/3 PASS.

- [ ] **Step 5: Commit**

```pwsh
git add IDEALAKEWMSService/Services/LagerplatzSyncService.cs IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs
git commit -m "feat(lagerplatz): update existing Sage-records on diff, no-op on identity" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 7: Test 4 (Konflikt: Source=Manual)

**Files:**
- Modify: `IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs`
- Modify: `IDEALAKEWMSService/Services/LagerplatzSyncService.cs`

- [ ] **Step 1: Test anhaengen**

```csharp
[Fact]
public async Task Run_ExistingManualRecord_SameCodeFromSage_ConflictWithoutWrite()
{
    var (svc, reader, ctx, syncLogs) = Build();
    ctx.StorageLocations.Add(new StorageLocation
    {
        Code = "ABC", Zone = "MANUAL-ZONE", Description = "Manuell angelegt",
        BarcodeValue = "ABC", Source = StorageLocationSource.Manual, IsActive = true,
        CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = "tester", CreatedByWindows = "tester"
    });
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("HALLE-1", "ABC", "Sage-Bezeichnung") };

    var result = await svc.RunAsync();

    result.Conflicts.Should().Be(1);
    result.Updated.Should().Be(0);

    var sl = ctx.StorageLocations.Single();
    sl.Source.Should().Be(StorageLocationSource.Manual);
    sl.Zone.Should().Be("MANUAL-ZONE");
    sl.Description.Should().Be("Manuell angelegt");

    var warnings = await syncLogs.GetRecentAsync("Lagerplatz", SyncLogLevel.Warning, 10);
    warnings.Should().ContainSingle();
    warnings[0].Reference.Should().Be("ABC");
    warnings[0].Message.Should().Contain("manuell");
}
```

- [ ] **Step 2: Test laufen — FAIL (Konflikt-Pfad fehlt)**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

Expected: 4. Test FAIL.

- [ ] **Step 3: Service-`else`-Block erweitern**

Den bestehenden `else`-Branch (aus Task 6) so umbauen, dass er auch den Manual-Fall behandelt:

```csharp
else
{
    var existingLoc = byCode[code];
    if (existingLoc.Source == StorageLocationSource.Sage)
    {
        // ... bestehende Diff/Update-Logik ...
    }
    else // Manual
    {
        await _syncLogs.AddAsync(new SyncLog
        {
            Service = ServiceName,
            Level = SyncLogLevel.Warning,
            Message = $"Konflikt: Lagerplatz {code} existiert manuell, Sage-Eintrag ignoriert.",
            Reference = code
        });
        conflicts++;
    }
}
```

- [ ] **Step 4: Test laufen — 4/4 PASS**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

- [ ] **Step 5: Commit**

```pwsh
git add IDEALAKEWMSService/Services/LagerplatzSyncService.cs IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs
git commit -m "feat(lagerplatz): log conflict when Sage-code matches Manual-record" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 8: Test 5 + 6 (Soft-Deactivate / Reaktivierung)

**Files:**
- Modify: `IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs`
- Modify: `IDEALAKEWMSService/Services/LagerplatzSyncService.cs`

- [ ] **Step 1: Tests anhaengen**

```csharp
[Fact]
public async Task Run_SageRecord_NoLongerInSage_SoftDeactivates()
{
    var (svc, reader, ctx, syncLogs) = Build();
    ctx.StorageLocations.Add(new StorageLocation
    {
        Code = "GONE-1", Zone = "HALLE-X", Description = "war mal in Sage",
        BarcodeValue = "GONE-1", Source = StorageLocationSource.Sage, IsActive = true,
        CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = SyncUser_For_Tests, CreatedByWindows = "x"
    });
    await ctx.SaveChangesAsync();
    reader.Records = new(); // leer

    var result = await svc.RunAsync();

    result.Deactivated.Should().Be(1);
    var sl = ctx.StorageLocations.Single();
    sl.IsActive.Should().BeFalse();
    sl.ModifiedAt.Should().NotBeNull();

    var infos = await syncLogs.GetRecentAsync("Lagerplatz", SyncLogLevel.Info, 10);
    infos.Should().ContainSingle(x => x.Reference == "GONE-1");
}

[Fact]
public async Task Run_DeactivatedSageRecord_ReappearsInSage_ReactivatesAndCountsUpdate()
{
    var (svc, reader, ctx, _) = Build();
    ctx.StorageLocations.Add(new StorageLocation
    {
        Code = "BACK-1", Zone = "HALLE-1", Description = "war mal weg",
        BarcodeValue = "BACK-1", Source = StorageLocationSource.Sage, IsActive = false,
        CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = SyncUser_For_Tests, CreatedByWindows = "x"
    });
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("HALLE-1", "BACK-1", "war mal weg") };

    var result = await svc.RunAsync();

    result.Updated.Should().Be(1);
    var sl = ctx.StorageLocations.Single();
    sl.IsActive.Should().BeTrue();
}
```

- [ ] **Step 2: Tests laufen — Soft-Deactivate FAIL, Reaktivierung evtl. zufaellig PASS (Diff-Detection erkennt IsActive-Wechsel bereits in Task 6)**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

- [ ] **Step 3: Soft-Deactivate-Pass nach der Hauptschleife einbauen**

Direkt nach der `foreach (var dto in sageRecords)`-Schleife, vor `await _ctx.SaveChangesAsync(ct)`:

```csharp
// Soft-deactivate Sage-Records, die nicht mehr in der Sage-Liste sind
var sageCodesInFeed = sageRecords
    .Select(r => string.IsNullOrWhiteSpace(r.Kurzbezeichnung) ? null : r.Kurzbezeichnung.Trim())
    .Where(c => c != null)
    .ToHashSet(StringComparer.OrdinalIgnoreCase)!;

foreach (var loc in existing.Where(l => l.Source == StorageLocationSource.Sage && l.IsActive))
{
    if (!sageCodesInFeed.Contains(loc.Code))
    {
        loc.IsActive = false;
        loc.ModifiedAt = DateTime.Now;
        loc.ModifiedBy = SyncUser;
        loc.ModifiedByWindows = Environment.MachineName;
        deactivated++;

        await _syncLogs.AddAsync(new SyncLog
        {
            Service = ServiceName,
            Level = SyncLogLevel.Info,
            Message = $"Lagerplatz {loc.Code} aus Sage entfernt -> deaktiviert.",
            Reference = loc.Code
        });
    }
}
```

- [ ] **Step 4: Tests laufen — alle PASS**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

Expected: 6/6 PASS.

- [ ] **Step 5: Commit**

```pwsh
git add IDEALAKEWMSService/Services/LagerplatzSyncService.cs IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs
git commit -m "feat(lagerplatz): soft-deactivate dropped Sage-records, reactivate on return" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 9: Test 7 (Sage-Duplicate-Detection)

**Files:**
- Modify: `IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs`
- Modify: `IDEALAKEWMSService/Services/LagerplatzSyncService.cs`

- [ ] **Step 1: Test anhaengen**

```csharp
[Fact]
public async Task Run_SageDuplicateCode_SkipsAllAndLogsWarning()
{
    var (svc, reader, ctx, syncLogs) = Build();
    reader.Records = new()
    {
        new("HALLE-A", "DUP", "Erste Zeile"),
        new("HALLE-B", "DUP", "Zweite Zeile"),
        new("HALLE-1", "OK",  "Eindeutiger Eintrag"),
    };

    var result = await svc.RunAsync();

    result.Inserted.Should().Be(1); // nur OK
    ctx.StorageLocations.Should().HaveCount(1);
    ctx.StorageLocations.Single().Code.Should().Be("OK");

    var warnings = await syncLogs.GetRecentAsync("Lagerplatz", SyncLogLevel.Warning, 10);
    warnings.Should().ContainSingle(x => x.Reference == "DUP");
    warnings[0].Message.Should().Contain("mehrfach");
}
```

- [ ] **Step 2: Test laufen — FAIL**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

- [ ] **Step 3: Duplicate-Detection vor der Hauptschleife einbauen**

Nach dem `try/catch` um `_reader.GetAllActiveAsync` und vor `var existing = await _ctx.StorageLocations.ToListAsync(ct);` einsetzen:

```csharp
// Sage-Duplikate erkennen und entfernen
var dupGroups = sageRecords
    .Where(r => !string.IsNullOrWhiteSpace(r.Kurzbezeichnung))
    .GroupBy(r => r.Kurzbezeichnung!.Trim(), StringComparer.OrdinalIgnoreCase)
    .Where(g => g.Count() > 1)
    .ToList();

foreach (var group in dupGroups)
{
    var bereiche = string.Join(", ", group.Select(g => g.Lagerkennung ?? "?"));
    await _syncLogs.AddAsync(new SyncLog
    {
        Service = ServiceName,
        Level = SyncLogLevel.Warning,
        Message = $"Sage liefert Lagerplatz '{group.Key}' mehrfach (Bereiche {bereiche}). Eintraege uebersprungen.",
        Reference = group.Key
    });
}

var dupCodes = dupGroups.Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
sageRecords = sageRecords
    .Where(r => string.IsNullOrWhiteSpace(r.Kurzbezeichnung)
             || !dupCodes.Contains(r.Kurzbezeichnung!.Trim()))
    .ToList();
```

- [ ] **Step 4: Test laufen — PASS**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

Expected: 7/7 PASS.

- [ ] **Step 5: Commit**

```pwsh
git add IDEALAKEWMSService/Services/LagerplatzSyncService.cs IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs
git commit -m "feat(lagerplatz): detect Sage-duplicate codes, skip and log warning" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 10: Tests 8 + 9 (Length-Cap Code / Description-Truncate)

**Files:**
- Modify: `IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs`
- Modify: `IDEALAKEWMSService/Services/LagerplatzSyncService.cs`

- [ ] **Step 1: Tests anhaengen**

```csharp
[Fact]
public async Task Run_SageCodeTooLong_SkipsAndLogsWarning()
{
    var (svc, reader, ctx, syncLogs) = Build();
    var longCode = new string('X', 51); // > 50
    reader.Records = new() { new("HALLE-1", longCode, "irgendwas") };

    var result = await svc.RunAsync();

    result.Inserted.Should().Be(0);
    result.Skipped.Should().Be(1);
    ctx.StorageLocations.Should().BeEmpty();

    var warnings = await syncLogs.GetRecentAsync("Lagerplatz", SyncLogLevel.Warning, 10);
    warnings.Should().Contain(x => x.Reference == longCode && x.Message.Contains("zu lang"));
}

[Fact]
public async Task Run_SageDescriptionTooLong_TruncatesAndLogsInfo()
{
    var (svc, reader, ctx, syncLogs) = Build();
    var longDesc = new string('Y', 250); // > 200
    reader.Records = new() { new("HALLE-1", "TRUNC-1", longDesc) };

    var result = await svc.RunAsync();

    result.Inserted.Should().Be(1);
    var sl = ctx.StorageLocations.Single();
    sl.Description!.Length.Should().Be(200);

    var infos = await syncLogs.GetRecentAsync("Lagerplatz", SyncLogLevel.Info, 10);
    infos.Should().Contain(x => x.Reference == "TRUNC-1" && x.Message.Contains("gekuerzt"));
}
```

- [ ] **Step 2: Tests laufen — beide FAIL**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

- [ ] **Step 3: Pre-Validierung in der Hauptschleife einbauen**

Im `foreach (var dto in sageRecords)`, GANZ AM ANFANG der Schleife (vor dem leeren Code-Check):

```csharp
if (!string.IsNullOrWhiteSpace(dto.Kurzbezeichnung))
{
    var rawCode = dto.Kurzbezeichnung.Trim();
    if (rawCode.Length > MaxCodeLength)
    {
        await _syncLogs.AddAsync(new SyncLog
        {
            Service = ServiceName,
            Level = SyncLogLevel.Warning,
            Message = $"Lagerplatz-Code '{rawCode}' ist zu lang ({rawCode.Length} > {MaxCodeLength}), uebersprungen.",
            Reference = rawCode
        });
        skipped++;
        continue;
    }
}
```

Plus: nach dem Einlesen des `code`-Variablenwerts und vor dem Insert/Update — Description-Truncate:

```csharp
var description = dto.Platzbezeichnung;
if (description != null && description.Length > MaxDescriptionLength)
{
    await _syncLogs.AddAsync(new SyncLog
    {
        Service = ServiceName,
        Level = SyncLogLevel.Info,
        Message = $"Beschreibung von '{code}' auf {MaxDescriptionLength} Zeichen gekuerzt.",
        Reference = code
    });
    description = description.Substring(0, MaxDescriptionLength);
}
```

Im Insert: `Description = description,` setzen.
Im Update-Diff-Vergleich: `existingLoc.Description != description` und `existingLoc.Description = description`.

- [ ] **Step 4: Tests laufen — alle PASS**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

Expected: 9/9 PASS.

- [ ] **Step 5: Commit**

```pwsh
git add IDEALAKEWMSService/Services/LagerplatzSyncService.cs IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs
git commit -m "feat(lagerplatz): pre-validate sage values (skip too-long code, truncate description)" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 11: Test 10 (Sage-Connection-Fehler)

**Files:**
- Modify: `IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs`

- [ ] **Step 1: Test anhaengen — Fehler-Pfad existiert bereits in Service**

```csharp
[Fact]
public async Task Run_SageReaderThrows_LogsError_NoCrash()
{
    var (svc, reader, ctx, syncLogs) = Build();
    reader.ThrowOnRead = new InvalidOperationException("Sage offline");

    var result = await svc.RunAsync();

    result.Errors.Should().Be(1);
    ctx.StorageLocations.Should().BeEmpty();

    var errors = await syncLogs.GetRecentAsync("Lagerplatz", SyncLogLevel.Error, 10);
    errors.Should().ContainSingle();
    errors[0].Message.Should().Contain("Sage offline");
}
```

- [ ] **Step 2: Test laufen — sollte direkt PASS, weil try/catch in Task 5 schon eingebaut**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

Expected: 10/10 PASS. Falls FAIL: Service-Code im Top-Level `try/catch` ergaenzen, bis PASS.

- [ ] **Step 3: Vollstaendigen Test-Lauf machen (alles)**

```pwsh
dotnet test --nologo
```

Expected: alle Tests in beiden Projekten gruen.

- [ ] **Step 4: Commit**

```pwsh
git add IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs
git commit -m "test(lagerplatz): cover sage-connection-failure path" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 3 — Worker-Integration

### Task 12: SyncWorker integriert + ServiceSetting-Default

**Files:**
- Modify: `IDEALAKEWMSService/Workers/SyncWorker.cs`
- Modify: `IDEALAKEWMSService/Program.cs` (DI fuer LagerplatzSyncService)
- Modify: `IDEALAKEWMSService/appsettings.json`

- [ ] **Step 1: Service in DI registrieren**

In `IDEALAKEWMSService/Program.cs`, im Services-Block, einfuegen:

```csharp
builder.Services.AddScoped<ILagerplatzSyncService, LagerplatzSyncService>();
```

- [ ] **Step 2: SyncWorker-Block ergaenzen**

In `IDEALAKEWMSService/Workers/SyncWorker.cs`, am Ende des `try`-Blocks (nach dem Holiday-Sync-Block, vor dem `catch (Exception ex) when (!stoppingToken.IsCancellationRequested)`):

```csharp
// ---------------------------------------------------------------
// Lagerplatz-Sync (Sage Stammdaten)
// ---------------------------------------------------------------
if (_configuration.GetValue<bool>("Sync:LagerplaetzeEnabled", false))
{
    try
    {
        _logger.LogInformation("Lagerplatz-Sync startet...");
        using var lpScope = _scopeFactory.CreateScope();
        var lpSync = lpScope.ServiceProvider.GetRequiredService<ILagerplatzSyncService>();
        var lpResult = await lpSync.RunAsync(stoppingToken);
        _logger.LogInformation(
            "Lagerplatz-Sync: {Inserted} neu, {Updated} aktualisiert, {Conflicts} Konflikte, {Deactivated} deaktiviert, {Skipped} uebersprungen, {Errors} Fehler.",
            lpResult.Inserted, lpResult.Updated, lpResult.Conflicts, lpResult.Deactivated, lpResult.Skipped, lpResult.Errors);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Lagerplatz-Sync ist fehlgeschlagen.");
    }
}
```

- [ ] **Step 3: Default in appsettings.json einfuegen**

In `IDEALAKEWMSService/appsettings.json`, im `Sync`-Block neue Zeile:

```json
"LagerplaetzeEnabled": false,
```

(vor `"BdeAutoPauseIntervalMinutes": 60,`)

- [ ] **Step 4: Build verifizieren**

```pwsh
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj --nologo
```

Expected: `0 Fehler`.

- [ ] **Step 5: Commit**

```pwsh
git add IDEALAKEWMSService/Program.cs IDEALAKEWMSService/Workers/SyncWorker.cs IDEALAKEWMSService/appsettings.json
git commit -m "feat(lagerplatz): integrate sync into SyncWorker, gated by Sync:LagerplaetzeEnabled" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 4 — Web-UI (Repository-Anpassungen + Views)

### Task 13: GetActiveAsync auf StorageLocationRepository + Tests

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IStorageLocationRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/StorageLocationRepository.cs`
- Create: `IdealAkeWms.Tests/Repositories/StorageLocationRepositoryTests.cs`

- [ ] **Step 1: Test schreiben**

```csharp
// IdealAkeWms.Tests/Repositories/StorageLocationRepositoryTests.cs
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Repositories;

public class StorageLocationRepositoryTests
{
    [Fact]
    public async Task GetActiveOrderedExcludingPickingTransport_FiltersInactiveAndPickingTransport()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageLocations.AddRange(
            New("A-01", isActive: true,  isPickingTransport: false),
            New("A-02", isActive: false, isPickingTransport: false),
            New("WAGEN-1", isActive: true, isPickingTransport: true)
        );
        await ctx.SaveChangesAsync();
        var repo = new StorageLocationRepository(ctx);

        var result = await repo.GetActiveOrderedExcludingPickingTransportAsync();

        result.Should().HaveCount(1);
        result[0].Code.Should().Be("A-01");
    }

    [Fact]
    public async Task GetActivePickingTransport_FiltersInactive()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageLocations.AddRange(
            New("WAGEN-1", isActive: true,  isPickingTransport: true),
            New("WAGEN-2", isActive: false, isPickingTransport: true)
        );
        await ctx.SaveChangesAsync();
        var repo = new StorageLocationRepository(ctx);

        var result = await repo.GetActivePickingTransportLocationsAsync();

        result.Should().ContainSingle().Which.Code.Should().Be("WAGEN-1");
    }

    private static StorageLocation New(string code, bool isActive, bool isPickingTransport) => new()
    {
        Code = code,
        BarcodeValue = code,
        IsActive = isActive,
        IsPickingTransport = isPickingTransport,
        Source = StorageLocationSource.Manual,
        CreatedBy = "tester",
        CreatedByWindows = "tester"
    };
}
```

- [ ] **Step 2: Tests laufen — FAIL (Methoden noch nicht da)**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~StorageLocationRepositoryTests" --nologo
```

- [ ] **Step 3: Interface erweitern**

In `IdealAkeWms/Data/Repositories/IStorageLocationRepository.cs`:

```csharp
Task<List<StorageLocation>> GetActiveOrderedExcludingPickingTransportAsync();
Task<List<StorageLocation>> GetActivePickingTransportLocationsAsync();
```

- [ ] **Step 4: Implementation**

In `IdealAkeWms/Data/Repositories/StorageLocationRepository.cs`:

```csharp
public async Task<List<StorageLocation>> GetActiveOrderedExcludingPickingTransportAsync()
{
    return await _dbSet
        .Where(sl => sl.IsActive && !sl.IsPickingTransport)
        .OrderBy(sl => sl.Code)
        .ToListAsync();
}

public async Task<List<StorageLocation>> GetActivePickingTransportLocationsAsync()
{
    return await _dbSet
        .Where(sl => sl.IsActive && sl.IsPickingTransport)
        .OrderBy(sl => sl.Code)
        .ToListAsync();
}
```

- [ ] **Step 5: Tests laufen — PASS**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~StorageLocationRepositoryTests" --nologo
```

- [ ] **Step 6: Commit**

```pwsh
git add IdealAkeWms/Data/Repositories/IStorageLocationRepository.cs IdealAkeWms/Data/Repositories/StorageLocationRepository.cs IdealAkeWms.Tests/Repositories/StorageLocationRepositoryTests.cs
git commit -m "feat(lagerplatz): add IsActive-aware repository queries" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 14: Konsumenten umstellen — StockMovements / Picking / StockOverview

**Files:**
- Modify: `IdealAkeWms/Controllers/StockMovementsController.cs` (alle `GetAllOrderedAsync`-Aufrufe)
- Modify: `IdealAkeWms/Controllers/PickingController.cs:192-193`
- Keep: `IdealAkeWms/Controllers/StockOverviewController.cs:52` (laesst inaktive sehen, soll bleiben)
- Keep: `IdealAkeWms/Controllers/StorageLocationsController.cs:25, :90` (Index zeigt alles, PrintLabels alles)

**Mapping-Tabelle (Begruendung im Commit-Body):**

| Datei | Stelle | Vorher | Nachher | Begruendung |
|---|---|---|---|---|
| StockMovementsController | Buchungs-Dropdowns (alle Indizes) | `GetAllOrderedAsync` | `GetActiveOrderedExcludingPickingTransportAsync` ODER `GetActivePickingTransportLocationsAsync` (je nach Kontext) | Buchungen auf inaktive Plaetze sperren |
| PickingController:192 | Stueckliste Quell-Dropdown | `GetAllOrderedExcludingPickingTransportAsync` | `GetActiveOrderedExcludingPickingTransportAsync` | inaktive Quellen ausblenden |
| PickingController:193 | Stueckliste Ziel-Dropdown | `GetPickingTransportLocationsAsync` | `GetActivePickingTransportLocationsAsync` | inaktive Wagen ausblenden |
| StockOverviewController:52 | Bestandsuebersicht Liste | `GetAllOrderedAsync` | `GetAllOrderedAsync` (BLEIBT) | Restbestaende auf inaktiven Plaetzen muessen sichtbar bleiben |
| StorageLocationsController | Index, PrintLabels | `GetAllOrderedAsync` | bleibt | Stammdaten-View zeigt alles inkl. inaktiv (mit Toggle, siehe Task 15) |

- [ ] **Step 1: PickingController umstellen**

In `IdealAkeWms/Controllers/PickingController.cs:192-193`:

```csharp
var allStorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync();
var targetStorageLocations = await _storageLocationRepository.GetActivePickingTransportLocationsAsync();
```

- [ ] **Step 2: StockMovementsController — alle Buchungs-Aufrufe umstellen**

Alle 10 Vorkommen in `IdealAkeWms/Controllers/StockMovementsController.cs` (Zeilen ~66, 81, 97, 150, 164, 188, 238, 256, 279, 328) auf `GetActiveOrderedExcludingPickingTransportAsync()` umstellen, sofern es um Buchungs-Dropdowns geht. Das `allLocations`-Statement in Zeile 402 (Bewegungshistorie) BLEIBT auf `GetAllOrderedAsync` — dort werden Bewegungen historisch angezeigt.

Pruefe per Grep:
```pwsh
```
und entscheide pro Stelle anhand des umliegenden Kontextes (Buchungs-View vs. History/Listing).

- [ ] **Step 3: Build + Tests**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --nologo
```

Expected: `0 Fehler`, alle Tests gruen.

- [ ] **Step 4: Commit**

```pwsh
git add IdealAkeWms/Controllers/StockMovementsController.cs IdealAkeWms/Controllers/PickingController.cs
git commit -m "fix(lagerplatz): hide inactive locations from booking + picking dropdowns" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 15: Lagerplaetze-Index — Quelle-Spalte + IsActive-Toggle

**Files:**
- Modify: `IdealAkeWms/Views/StorageLocations/Index.cshtml`
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` (falls Spaltenkonfiguration zentral)
- Modify: `IdealAkeWms/Controllers/StorageLocationsController.cs` (Index-Action mit `bool showInactive = false` Parameter)

- [ ] **Step 1: Index-Action erweitern**

In `IdealAkeWms/Controllers/StorageLocationsController.cs`:

```csharp
public async Task<IActionResult> Index(bool showInactive = false)
{
    var all = await _storageLocationRepository.GetAllOrderedAsync();
    var locations = showInactive ? all : all.Where(l => l.IsActive).ToList();
    ViewBag.ShowInactive = showInactive;
    ViewBag.HasInactive = all.Any(l => !l.IsActive);
    return View(locations);
}
```

- [ ] **Step 2: View aktualisieren**

In `IdealAkeWms/Views/StorageLocations/Index.cshtml`:
- Direkt nach der Page-Header-Zeile, einen Toggle einfuegen:

```cshtml
@if ((bool?)ViewBag.HasInactive == true)
{
    <form method="get" class="mb-3">
        <div class="form-check form-switch">
            <input class="form-check-input" type="checkbox" id="showInactiveToggle" name="showInactive" value="true"
                   @(ViewBag.ShowInactive == true ? "checked" : "")
                   onchange="this.form.submit()" />
            <label class="form-check-label" for="showInactiveToggle">Auch inaktive Lagerpl&auml;tze zeigen</label>
        </div>
    </form>
}
```

- In der Tabellen-Header-Sektion (`<thead>`) eine neue `<th>` mit `data-col-key="Source"` "Quelle" einfuegen, und (sofern `ShowInactive`) eine `<th data-col-key="IsActive">Aktiv</th>`.

- In der `<tbody>`-Zeile pro Lagerplatz:
  - Nach den bestehenden Spalten eine `<td>`:

```cshtml
<td data-col-key="Source">
    @if (item.Source == IdealAkeWms.Models.StorageLocationSource.Sage)
    {
        <span class="badge bg-info">Sage</span>
    }
    else
    {
        <span class="badge bg-secondary">Manuell</span>
    }
</td>
```

  - Wenn `ShowInactive`: zusaetzlich `<td data-col-key="IsActive">@(item.IsActive ? "Ja" : "Nein")</td>`.

  - Optisches Highlight fuer inaktive Zeilen: `<tr class="@(item.IsActive ? "" : "text-muted text-decoration-line-through")">` oder eine CSS-Klasse, falls die existierende Tabellenstruktur es zulaesst.

- [ ] **Step 3: Spalten-Keys in `ColumnDefinitions.cs` ergaenzen**

Wenn die Klasse `ColumnDefinitions` Spaltenkeys zentral pflegt (per CLAUDE.md-Fallstricke `data-col-key Pflicht`), die neuen Keys `"Source"` und `"IsActive"` dort fuer den StorageLocations-Kontext hinzufuegen. Die aktuelle Datei aufmachen und entsprechende Konstanten an passender Stelle ergaenzen.

- [ ] **Step 4: Manuell pruefen**

```pwsh
dotnet run --project IdealAkeWms/IdealAkeWms.csproj
```

In Browser: Stammdaten -> Lagerplaetze. Toggle erscheint nur wenn inaktive Plaetze existieren. Quelle-Spalte zeigt Badge je Datensatz.

- [ ] **Step 5: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test IdealAkeWms.Tests --nologo
```

- [ ] **Step 6: Commit**

```pwsh
git add IdealAkeWms/Controllers/StorageLocationsController.cs IdealAkeWms/Views/StorageLocations/Index.cshtml IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
git commit -m "feat(lagerplatz): show source + inactive toggle on storage-locations index" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 16: Edit-Maske + Server-Side-Schutz fuer Sage-Records

**Files:**
- Modify: `IdealAkeWms/Controllers/StorageLocationsController.cs:60-86` (Edit-POST)
- Modify: `IdealAkeWms/Views/StorageLocations/Edit.cshtml`
- Modify: `IdealAkeWms/Views/StorageLocations/Create.cshtml` (nur falls noetig — Source bei Create immer Manual)
- Create: `IdealAkeWms.Tests/Controllers/StorageLocationsControllerTests.cs`

- [ ] **Step 1: Tests schreiben**

```csharp
// IdealAkeWms.Tests/Controllers/StorageLocationsControllerTests.cs
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class StorageLocationsControllerTests
{
    [Fact]
    public async Task Edit_Post_SourceSage_IgnoresCodeZoneDescriptionAndIsActive()
    {
        using var ctx = TestDbContextFactory.Create();
        var existing = new StorageLocation
        {
            Code = "S-01", Zone = "HALLE-1", Description = "Sage-Beschreibung",
            BarcodeValue = "S-01", Source = StorageLocationSource.Sage, IsActive = true,
            Capacity = null, IsPickingTransport = false,
            CreatedBy = "x", CreatedByWindows = "x"
        };
        ctx.StorageLocations.Add(existing);
        await ctx.SaveChangesAsync();

        var repo = new StorageLocationRepository(ctx);
        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(x => x.GetDisplayName()).Returns("admin");
        userSvc.Setup(x => x.GetWindowsUserName()).Returns("admin");
        var ctrl = new StorageLocationsController(repo, userSvc.Object);

        var posted = new StorageLocation
        {
            Id = existing.Id,
            Code = "HACKED",
            Zone = "BAD",
            Description = "BAD",
            IsActive = false,                  // Versuch zu deaktivieren
            Capacity = 99,                     // erlaubt
            IsPickingTransport = true          // erlaubt
        };

        var result = await ctrl.Edit(existing.Id, posted);
        result.Should().BeOfType<RedirectToActionResult>();

        var saved = ctx.StorageLocations.Single();
        saved.Code.Should().Be("S-01");
        saved.Zone.Should().Be("HALLE-1");
        saved.Description.Should().Be("Sage-Beschreibung");
        saved.IsActive.Should().BeTrue();
        saved.Capacity.Should().Be(99);
        saved.IsPickingTransport.Should().BeTrue();
    }

    [Fact]
    public async Task Edit_Post_SourceManual_AcceptsAllFields_IncludingIsActive()
    {
        using var ctx = TestDbContextFactory.Create();
        var existing = new StorageLocation
        {
            Code = "M-01", Zone = "Z1", Description = "Manuell",
            BarcodeValue = "M-01", Source = StorageLocationSource.Manual, IsActive = true,
            CreatedBy = "x", CreatedByWindows = "x"
        };
        ctx.StorageLocations.Add(existing);
        await ctx.SaveChangesAsync();

        var repo = new StorageLocationRepository(ctx);
        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(x => x.GetDisplayName()).Returns("admin");
        userSvc.Setup(x => x.GetWindowsUserName()).Returns("admin");
        var ctrl = new StorageLocationsController(repo, userSvc.Object);

        var posted = new StorageLocation
        {
            Id = existing.Id,
            Code = "M-01-NEU",
            Zone = "Z2",
            Description = "Geaendert",
            IsActive = false,
            Capacity = 5,
            IsPickingTransport = false
        };

        await ctrl.Edit(existing.Id, posted);

        var saved = ctx.StorageLocations.Single();
        saved.Code.Should().Be("M-01-NEU");
        saved.Zone.Should().Be("Z2");
        saved.Description.Should().Be("Geaendert");
        saved.IsActive.Should().BeFalse();
        saved.BarcodeValue.Should().Be("M-01-NEU");
    }
}
```

- [ ] **Step 2: Tests laufen — FAIL (Controller akzeptiert noch alle Felder)**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~StorageLocationsControllerTests" --nologo
```

- [ ] **Step 3: Edit-POST anpassen**

In `IdealAkeWms/Controllers/StorageLocationsController.cs`, den Block `existing.Code = location.Code; ...` ersetzen:

```csharp
if (existing.Source == StorageLocationSource.Sage)
{
    // Sage-kontrollierte Felder bleiben unangetastet — der Sync ist Master.
    existing.Capacity = location.Capacity;
    existing.IsPickingTransport = location.IsPickingTransport;
    // IsActive ist Sync-kontrolliert: NICHT aus dem POST uebernehmen.
}
else
{
    existing.Code = location.Code;
    existing.Description = location.Description;
    existing.Zone = location.Zone;
    existing.Capacity = location.Capacity;
    existing.IsPickingTransport = location.IsPickingTransport;
    existing.IsActive = location.IsActive;
    existing.BarcodeValue = location.Code;
}

existing.ModifiedAt = DateTime.Now;
existing.ModifiedBy = _currentUserService.GetDisplayName();
existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();
```

- [ ] **Step 4: Tests laufen — PASS**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~StorageLocationsControllerTests" --nologo
```

- [ ] **Step 5: Edit.cshtml visuell anpassen**

In `IdealAkeWms/Views/StorageLocations/Edit.cshtml`:

- Wenn `Model.Source == "Sage"`: Banner oben einfuegen:

```cshtml
@if (Model.Source == IdealAkeWms.Models.StorageLocationSource.Sage)
{
    <div class="alert alert-info">
        Dieser Lagerplatz wird aus Sage synchronisiert. <strong>Code</strong>, <strong>Bereich/Zone</strong>, <strong>Bezeichnung</strong> und <strong>Aktiv</strong> sind gesperrt — sie werden beim n&auml;chsten Abgleich aus Sage uebernommen.
    </div>
}
```

- Bei den Feldern Code/Description/Zone die Inputs mit `readonly`-Attribut versehen wenn `Model.Source == "Sage"`. Bei IsActive die Checkbox auf `disabled` setzen + Hidden-Input mit aktuellem Wert (wegen Form-Bind), damit nichts ueberschrieben wird (der Server-Side-Schutz aus Step 3 ist die Hauptverteidigung).

- [ ] **Step 6: Build + alle Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: alles gruen.

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms/Controllers/StorageLocationsController.cs IdealAkeWms/Views/StorageLocations/Edit.cshtml IdealAkeWms.Tests/Controllers/StorageLocationsControllerTests.cs
git commit -m "feat(lagerplatz): protect sage-fields server-side + read-only banner in edit view" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 17: Bestandsuebersicht — Warning-Badge fuer inaktive Plaetze mit Bestand

**Files:**
- Modify: `IdealAkeWms/Views/StockOverview/Index.cshtml` (oder die View, die die Bestandsliste rendert — vorher mit Read pruefen)

- [ ] **Step 1: Pfad und View-Variable verifizieren**

```pwsh
```

In `IdealAkeWms/Views/StockOverview/Index.cshtml` die Stelle finden, wo der Lagerplatz pro Zeile angezeigt wird (typischerweise als Spalte "Lagerplatz").

- [ ] **Step 2: Anpassung in der View**

Pro Zeile den Lagerplatz-Code-Renderer um ein Badge erweitern:

```cshtml
@item.StorageLocationCode
@if (item.StorageLocationIsActive == false && item.Quantity > 0)
{
    <span class="badge bg-warning text-dark ms-1" title="Lagerplatz ist deaktiviert (Sage), Bestand sollte umgebucht werden">inaktiv</span>
}
```

Ggf. das ViewModel um `StorageLocationIsActive` erweitern (Repository/Aggregation entsprechend anpassen). Vor dem Code-Aenderung pruefen, ob das ViewModel bereits einen Pfad hat (z.B. `item.StorageLocation.IsActive`).

- [ ] **Step 3: Manuell verifizieren**

App starten, Inaktiver Lagerplatz mit Bestand erstellen, Bestandsuebersicht oeffnen, Warning-Badge erscheint.

- [ ] **Step 4: Build**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

- [ ] **Step 5: Commit**

```pwsh
git add IdealAkeWms/Views/StockOverview/Index.cshtml IdealAkeWms/Models/ViewModels/StockOverviewViewModel.cs
git commit -m "feat(lagerplatz): warn-badge for inactive locations with remaining stock" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 5 — Sync-Protokoll-View

### Task 18: SyncLogController + Index-View + Layout-Menue

**Files:**
- Create: `IdealAkeWms/Controllers/SyncLogController.cs`
- Create: `IdealAkeWms/Models/ViewModels/SyncLogIndexViewModel.cs`
- Create: `IdealAkeWms/Views/SyncLog/Index.cshtml`
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml` (Menue-Eintrag)
- Create: `IdealAkeWms.Tests/Controllers/SyncLogControllerTests.cs`

- [ ] **Step 1: ViewModel**

```csharp
// IdealAkeWms/Models/ViewModels/SyncLogIndexViewModel.cs
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class SyncLogIndexViewModel
{
    public List<SyncLog> Entries { get; set; } = new();
    public string? FilterService { get; set; }
    public string? FilterLevel { get; set; }
    public string? FilterReference { get; set; }
    public List<string> AvailableServices { get; set; } = new();
}
```

- [ ] **Step 2: Test schreiben**

```csharp
// IdealAkeWms.Tests/Controllers/SyncLogControllerTests.cs
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Tests.Controllers;

public class SyncLogControllerTests
{
    [Fact]
    public async Task Index_FiltersByServiceAndLevel()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.SyncLogs.AddRange(
            new SyncLog { Service = "Lagerplatz", Level = SyncLogLevel.Info,    Message = "A" },
            new SyncLog { Service = "Lagerplatz", Level = SyncLogLevel.Warning, Message = "B" },
            new SyncLog { Service = "OseonTracking", Level = SyncLogLevel.Warning, Message = "C" }
        );
        await ctx.SaveChangesAsync();
        var repo = new SyncLogRepository(ctx);
        var ctrl = new SyncLogController(repo);

        var result = await ctrl.Index(service: "Lagerplatz", level: SyncLogLevel.Warning, reference: null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var vm = view.Model.Should().BeOfType<SyncLogIndexViewModel>().Subject;
        vm.Entries.Should().ContainSingle();
        vm.Entries[0].Message.Should().Be("B");
    }
}
```

- [ ] **Step 3: Tests laufen — FAIL**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~SyncLogControllerTests" --nologo
```

- [ ] **Step 4: Controller**

```csharp
// IdealAkeWms/Controllers/SyncLogController.cs
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireMasterDataAccess]
public class SyncLogController : Controller
{
    private const int PageSize = 200;
    private static readonly string[] KnownServices = new[] { "Lagerplatz", "OseonTracking", "Article", "ProductionOrder", "EnaioDms", "BomCache", "Holiday" };

    private readonly ISyncLogRepository _repo;

    public SyncLogController(ISyncLogRepository repo)
    {
        _repo = repo;
    }

    public async Task<IActionResult> Index(string? service, string? level, string? reference)
    {
        var entries = await _repo.GetRecentAsync(service, level, PageSize);
        if (!string.IsNullOrWhiteSpace(reference))
            entries = entries.Where(e => e.Reference != null && e.Reference.Contains(reference, StringComparison.OrdinalIgnoreCase)).ToList();

        return View(new SyncLogIndexViewModel
        {
            Entries = entries,
            FilterService = service,
            FilterLevel = level,
            FilterReference = reference,
            AvailableServices = KnownServices.ToList()
        });
    }
}
```

- [ ] **Step 5: View**

```cshtml
@* IdealAkeWms/Views/SyncLog/Index.cshtml *@
@model SyncLogIndexViewModel
@{
    ViewData["Title"] = "Sync-Protokoll";
}
<h2 class="page-header">Sync-Protokoll</h2>

<form method="get" class="row g-2 mb-3">
    <div class="col-md-3">
        <label class="form-label">Service</label>
        <select name="service" class="form-select" onchange="this.form.submit()">
            <option value="">— alle —</option>
            @foreach (var s in Model.AvailableServices)
            {
                <option value="@s" selected="@(s == Model.FilterService)">@s</option>
            }
        </select>
    </div>
    <div class="col-md-3">
        <label class="form-label">Stufe</label>
        <select name="level" class="form-select" onchange="this.form.submit()">
            <option value="">— alle —</option>
            <option value="Info"    selected="@(Model.FilterLevel == "Info")">Info</option>
            <option value="Warning" selected="@(Model.FilterLevel == "Warning")">Warning</option>
            <option value="Error"   selected="@(Model.FilterLevel == "Error")">Error</option>
        </select>
    </div>
    <div class="col-md-3">
        <label class="form-label">Referenz</label>
        <input type="text" name="reference" class="form-control" value="@Model.FilterReference" />
    </div>
    <div class="col-md-3 align-self-end">
        <button type="submit" class="btn btn-primary">Filtern</button>
        <a class="btn btn-outline-secondary" asp-action="Index">Zur&uuml;cksetzen</a>
    </div>
</form>

<div class="table-responsive">
    <table class="table table-striped table-sm">
        <thead>
            <tr>
                <th data-col-key="Timestamp">Zeitpunkt</th>
                <th data-col-key="Service">Service</th>
                <th data-col-key="Level">Stufe</th>
                <th data-col-key="Reference">Referenz</th>
                <th data-col-key="Message">Meldung</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var e in Model.Entries)
            {
                <tr>
                    <td>@e.Timestamp.ToString("dd.MM.yyyy HH:mm:ss")</td>
                    <td>@e.Service</td>
                    <td>
                        @switch (e.Level)
                        {
                            case "Error":   <span class="badge bg-danger">Error</span>   break;
                            case "Warning": <span class="badge bg-warning text-dark">Warning</span> break;
                            default:        <span class="badge bg-info">Info</span>      break;
                        }
                    </td>
                    <td>@e.Reference</td>
                    <td>@e.Message</td>
                </tr>
            }
            @if (!Model.Entries.Any())
            {
                <tr><td colspan="5" class="text-center text-muted py-3">Keine Eintraege im Filter.</td></tr>
            }
        </tbody>
    </table>
</div>
```

- [ ] **Step 6: Layout-Menue ergaenzen**

In `IdealAkeWms/Views/Shared/_Layout.cshtml`, im Stammdaten-Dropdown (`<ul class="dropdown-menu">`) nach dem Eintrag fuer Service-Einstellungen (Zeile ~178):

```cshtml
<li><a class="dropdown-item" asp-controller="SyncLog" asp-action="Index">Sync-Protokoll</a></li>
```

- [ ] **Step 7: Tests laufen — PASS**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~SyncLogControllerTests" --nologo
```

- [ ] **Step 8: Build + manueller Smoke-Test**

```pwsh
dotnet build --nologo
dotnet run --project IdealAkeWms/IdealAkeWms.csproj
```

In Browser: Stammdaten -> Sync-Protokoll. Filter funktioniert.

- [ ] **Step 9: Commit**

```pwsh
git add IdealAkeWms/Controllers/SyncLogController.cs IdealAkeWms/Models/ViewModels/SyncLogIndexViewModel.cs IdealAkeWms/Views/SyncLog/Index.cshtml IdealAkeWms/Views/Shared/_Layout.cshtml IdealAkeWms.Tests/Controllers/SyncLogControllerTests.cs
git commit -m "feat(sync): SyncLog read-only view with service/level/reference filter" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 6 — Doku + Versionierung

### Task 19: Hilfeseite + AppVersion + Changelog + PROJECT_STATUS + CLAUDE.md

**Files:**
- Modify: `IdealAkeWms/Views/Help/Index.cshtml` (neuer Abschnitt)
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `PROJECT_STATUS.md`
- Modify: `CLAUDE.md`

- [ ] **Step 1: AppVersion auf v1.9.0 setzen**

In beiden `AppVersion.cs`-Dateien: `Version = "1.9.0"`, `Date = "<heutiges Datum>"`.

- [ ] **Step 2: Changelog-Eintrag**

Im obersten Card-Block in `IdealAkeWms/Views/Help/Changelog.cshtml`:

```cshtml
<div class="card mb-3">
    <div class="card-header text-white" style="background-color: var(--ake-primary);">
        <strong>v1.9.0</strong> <span class="text-white-50 ms-2"><heutiges Datum></span>
    </div>
    <div class="card-body">
        <h6>Sage Lagerplatz-Sync (Phase 1)</h6>
        <ul>
            <li><strong>Automatischer Sync</strong> der Sage-Lagerplaetze (Bereich/Zone, Code, Bezeichnung) im Worker-Service. Aktivieren via ServiceSetting <code>Sync:LagerplaetzeEnabled</code>.</li>
            <li>Lagerplaetze haben jetzt ein <strong>Quelle</strong>-Kennzeichen (Manuell / Sage). Bei Sage-Records sind Code/Zone/Bezeichnung im UI gesperrt.</li>
            <li>Inaktive Sage-Lagerplaetze werden ausgeblendet — Toggle "Auch inaktive zeigen" auf der Lagerplaetze-Liste.</li>
            <li>Buchungen auf inaktive Lagerplaetze werden im UI nicht mehr angeboten. Bestaende auf inaktiven Plaetzen werden als <em>inaktiv</em>-Badge in der Bestandsuebersicht markiert.</li>
            <li>Neue Stammdaten-Sicht: <strong>Sync-Protokoll</strong> (zeigt Konflikte, Deaktivierungen und Sync-Lauf-Zusammenfassungen).</li>
        </ul>
    </div>
</div>
```

- [ ] **Step 3: Hilfeseite-Abschnitt**

In `IdealAkeWms/Views/Help/Index.cshtml`, an passender Stelle (nach Stammdaten-Sektion) einen neuen Abschnitt einfuegen:

```cshtml
<div class="card mb-3">
    <div class="card-header" style="background-color: var(--ake-primary); color: white;">
        Lagerplatz-Sync mit Sage
    </div>
    <div class="card-body">
        <p>Lagerplatz-Stammdaten werden aus Sage automatisch synchronisiert. Aktiviert wird das in den Service-Einstellungen unter <code>Sync:LagerplaetzeEnabled</code>.</p>
        <h6>Was wird synchronisiert</h6>
        <ul>
            <li>Code (Sage: Kurzbezeichnung), Bereich/Zone (Sage: Lagerkennung), Bezeichnung (Sage: Platzbezeichnung).</li>
            <li>Lagerplaetze, die in Sage deaktiviert werden, werden im WMS deaktiviert — bleiben fuer Historie/Bestand sichtbar, lassen aber keine neuen Buchungen mehr zu.</li>
        </ul>
        <h6>Quelle-Kennzeichen</h6>
        <p>Jeder Lagerplatz hat eine Quelle: <strong>Manuell</strong> (von einem Anwender im WMS angelegt) oder <strong>Sage</strong> (vom Sync importiert). Bei Sage-Quelle sind Code/Zone/Bezeichnung gesperrt — sie werden beim naechsten Abgleich ueberschrieben.</p>
        <h6>Konflikte</h6>
        <p>Wenn ein manueller Lagerplatz denselben Code wie ein Sage-Eintrag hat, wird der Sage-Eintrag ignoriert und ein Eintrag im <a asp-controller="SyncLog" asp-action="Index">Sync-Protokoll</a> als <em>Warning</em> erfasst. Aufloesung: in Absprache mit dem Sage-Verantwortlichen entscheiden, ob der manuelle Lagerplatz geloescht/umbenannt oder direkt in der DB als Sage-Quelle markiert werden soll.</p>
        <h6>Vor erstem Aktivieren</h6>
        <p>Wenn das WMS bereits Lagerplaetze hat, deren Codes auch in Sage existieren: vor dem ersten Aktivieren des Syncs <code>Source = 'Sage'</code> auf diesen Records direkt in der DB setzen, sonst werden alle als Konflikt geloggt.</p>
    </div>
</div>
```

- [ ] **Step 4: PROJECT_STATUS.md**

Roadmap-Sektion ergaenzen:

```markdown
- v1.9.0 (2026-05-XX) — Sage Lagerplatz-Sync (Phase 1, Stammdaten). Phase 2 (Lagerbestand-Uebernahme) folgt.
```

- [ ] **Step 5: CLAUDE.md**

Im ServiceSettings-Block die neue Zeile ergaenzen:

```markdown
| `Sync:LagerplaetzeEnabled` | `false` | Sage-Lagerplatz-Stammdaten-Sync aktiv |
```

- [ ] **Step 6: Build + alle Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: alles gruen. Erwartete Test-Anzahl: ~520-540 (10 neue Sync-Tests + 2 Repo-Tests + 2 Controller-Tests + 1 SyncLog-Controller + 2 SyncLog-Repo).

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml IdealAkeWms/Views/Help/Index.cshtml PROJECT_STATUS.md CLAUDE.md
git commit -m "docs: v1.9.0 — Sage Lagerplatz-Sync release notes + help page" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Manuelle Verifikation (vor Merge)

Diese Schritte sind keine Tasks im Plan — aber beim Abschluss zu erledigen:

- **Etikettendruck-Smoke-Test:** Sage-importierten Lagerplatz oeffnen, Etikett drucken, Barcode scannen — funktioniert wie zuvor.
- **DryRun-Test gegen echte Sage-DB:** `Sync:LagerplaetzeEnabled=true` + `WorkerSettings:SyncDryRun=true`. Worker-Log auf "Sage liefert N Lagerplaetze" pruefen, kein Schreibzugriff auf WMS-DB.
- **Mandant-Filter verifizieren:** Falls Sage-Schema kein `Mandant` auf `KHKLagerorte` hat → Filter aus dem SQL entfernen + Anpassung committen.
- **Konflikt-Tsunami-Pre-Check:** Wenn produktive WMS-Lagerplaetze vorhanden sind, vor erstem Aktivieren des Syncs SQL-Skript ausfuehren, das `Source` der bekannten Sage-Codes setzt.

---

## Self-Review-Notiz

Der Plan deckt die Spec-Sektionen vollstaendig:
- Datenmodell + Migrationen → Tasks 1-2
- ISageLagerplatzReader (Mock-Grenze) → Task 4
- Sync-Algorithmus mit allen 10 Test-Szenarien → Tasks 5-11
- Worker-Integration + ServiceSetting-Default → Task 12
- Repository GetActiveAsync + Konsumenten-Audit → Tasks 13-14
- Index-View Quelle + IsActive-Toggle → Task 15
- Edit-Maske Server-Side-Schutz → Task 16
- Bestandsuebersicht-Badge → Task 17
- SyncLog-View + Layout-Menue → Task 18
- Hilfeseite + Changelog + AppVersion + PROJECT_STATUS + CLAUDE.md → Task 19
