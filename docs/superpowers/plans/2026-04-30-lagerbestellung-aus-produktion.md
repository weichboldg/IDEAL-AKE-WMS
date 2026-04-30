# Lagerbestellung aus der Produktion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produktion erfasst eine Lagerbestellung mit 1..N Lagerartikeln, Lager kommissioniert mit Print + Pro-Position-Ist-Mengen und schließt ab. Submit triggert Mail an konfigurierten Default-Empfänger; Storno triggert `[STORNO]`-Mail wenn vorher submitted.

**Architecture:** Eigene Aggregat-Entity `WarehouseRequisition` (Header) + `WarehouseRequisitionItem` (Items) mit Lifecycle Draft → Submitted → Closed plus Cancelled-Branch. Eigene MVC-Controller Erfasser (`WarehouseRequisitionsController`) und Lager (`WarehousePickingController`), eigener Worker-Service (`WarehouseRequisitionEmailService`). Bestehende Infrastruktur wiederverwendet: `OrderRecipientGroup`, `ArticleRepository.SearchAsync`, `ProductionWorkplaceUser`, `MailKit`-basierter `IMailService`.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10, SQL Server, MailKit/MimeKit für SMTP, xUnit + FluentAssertions + Moq + EF InMemory, Bootstrap 5.

**Scope:** Worktree `C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1`, Branch `feature/bde-phase-1`. Versions-Bump v1.8.3 → v1.8.4.

**Spec-Anchor-Korrekturen vs. Spec-Doc:**
- `PartRequisitionEmailService` nutzt **raw ADO.NET**. Wir nutzen für `WarehouseRequisitionEmailService` **EF Core** (sauberer, da `ApplicationDbContext` seit Phase 2.3 im Service-Projekt verfügbar).
- SMTP-Config-Keys sind **`MailSettings:*`** (nicht `Notifications:*`). `IMailService.SendAsync(...)` ist abstrakt — Service-Projekt registriert ihn bereits via DI.
- `Notifications:AppBaseUrl` wird via `ServiceSettings` DB-Tabelle gelesen (Pattern aus `NotificationWorker.GetServiceSettingAsync`).
- `IAppSettingRepository` hat `GetIntValueAsync(key, defaultValue)` — perfekt für unseren `DefaultLagerbestellempfaengerId`-Lookup.

---

## Task 1: Datenmodell — Entities + Enum + EF-Konfig + Migration + SQL/53 + FreshInstall

**Files:**
- Create: `IdealAkeWms/Models/WarehouseRequisitionStatus.cs`
- Create: `IdealAkeWms/Models/WarehouseRequisition.cs`
- Create: `IdealAkeWms/Models/WarehouseRequisitionItem.cs`
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs`
- Create: `IdealAkeWms/Migrations/<ts>_AddWarehouseRequisitions.cs` (via EF CLI)
- Create: `SQL/53_AddWarehouseRequisitions.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: Status-Enum anlegen**

`IdealAkeWms/Models/WarehouseRequisitionStatus.cs`:

```csharp
namespace IdealAkeWms.Models;

public enum WarehouseRequisitionStatus : byte
{
    Draft     = 1,
    Submitted = 2,
    Closed    = 3,
    Cancelled = 4
}
```

- [ ] **Step 2: Header-Entity anlegen**

`IdealAkeWms/Models/WarehouseRequisition.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class WarehouseRequisition : AuditableEntity
{
    [Required]
    public int ProductionWorkplaceId { get; set; }
    public ProductionWorkplace ProductionWorkplace { get; set; } = null!;

    public WarehouseRequisitionStatus Status { get; set; } = WarehouseRequisitionStatus.Draft;

    public int? OrderRecipientGroupId { get; set; }
    public OrderRecipientGroup? OrderRecipientGroup { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public int? SubmittedByUserId { get; set; }

    public DateTime? ClosedAt { get; set; }
    public int? ClosedByUserId { get; set; }

    public DateTime? CancelledAt { get; set; }
    public int? CancelledByUserId { get; set; }

    [StringLength(500)]
    public string? CancellationReason { get; set; }

    public DateTime? EmailSentAt { get; set; }
    public DateTime? CancellationEmailSentAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<WarehouseRequisitionItem> Items { get; set; } = new List<WarehouseRequisitionItem>();
}
```

- [ ] **Step 3: Item-Entity anlegen**

`IdealAkeWms/Models/WarehouseRequisitionItem.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class WarehouseRequisitionItem : AuditableEntity
{
    [Required]
    public int WarehouseRequisitionId { get; set; }
    public WarehouseRequisition WarehouseRequisition { get; set; } = null!;

    [Required, StringLength(100)]
    public string ArticleNumber { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string ArticleDescription { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Unit { get; set; }

    public decimal QuantityRequested { get; set; }
    public decimal? QuantityPicked { get; set; }

    public int Position { get; set; }
}
```

- [ ] **Step 4: EF-Konfig in `ApplicationDbContext.cs`**

DbSets ergänzen (bei den anderen DbSets):

```csharp
    public DbSet<WarehouseRequisition> WarehouseRequisitions => Set<WarehouseRequisition>();
    public DbSet<WarehouseRequisitionItem> WarehouseRequisitionItems => Set<WarehouseRequisitionItem>();
```

Im `OnModelCreating` (am Ende, nach den anderen Konfigs):

```csharp
        modelBuilder.Entity<WarehouseRequisition>(entity =>
        {
            entity.ToTable("WarehouseRequisitions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CancellationReason).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ProductionWorkplaceId);
            entity.HasIndex(e => e.SubmittedAt);

            entity.HasOne(e => e.ProductionWorkplace)
                .WithMany()
                .HasForeignKey(e => e.ProductionWorkplaceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.OrderRecipientGroup)
                .WithMany()
                .HasForeignKey(e => e.OrderRecipientGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WarehouseRequisitionItem>(entity =>
        {
            entity.ToTable("WarehouseRequisitionItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ArticleNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ArticleDescription).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.QuantityRequested).HasColumnType("decimal(18,4)");
            entity.Property(e => e.QuantityPicked).HasColumnType("decimal(18,4)");
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => new { e.WarehouseRequisitionId, e.Position });
            entity.HasIndex(e => new { e.WarehouseRequisitionId, e.ArticleNumber }).IsUnique();

            entity.HasOne(e => e.WarehouseRequisition)
                .WithMany(r => r.Items)
                .HasForeignKey(e => e.WarehouseRequisitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
```

- [ ] **Step 5: EF-Migration generieren**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms" && dotnet ef migrations add AddWarehouseRequisitions
```

Erwartet: neue Datei `Migrations/<ts>_AddWarehouseRequisitions.cs`.

- [ ] **Step 6: Migration prüfen**

```bash
ls "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Migrations/" | grep -i AddWarehouseRequisitions
cat "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Migrations/"*_AddWarehouseRequisitions.cs | head -80
```

Expected: `Up()` enthält `CreateTable` für beide neuen Tabellen, `CreateIndex` für die 5 neuen Indizes (Status, ProductionWorkplaceId, SubmittedAt, RequisitionId+Position, RequisitionId+ArticleNumber UNIQUE), `RowVersion` ist `rowversion`-Spalte. Kein unrelated Drift.

Falls Drift → Migration removen, beheben, neu generieren.

- [ ] **Step 7: SQL/53 anlegen**

`SQL/53_AddWarehouseRequisitions.sql`:

```sql
-- Phase: Lagerbestellung v1.8.4
-- Idempotent: WarehouseRequisitions + WarehouseRequisitionItems anlegen.

IF OBJECT_ID('dbo.WarehouseRequisitions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarehouseRequisitions (
        Id int IDENTITY(1,1) PRIMARY KEY,
        ProductionWorkplaceId int NOT NULL,
        Status tinyint NOT NULL,
        OrderRecipientGroupId int NULL,
        SubmittedAt datetime2 NULL,
        SubmittedByUserId int NULL,
        ClosedAt datetime2 NULL,
        ClosedByUserId int NULL,
        CancelledAt datetime2 NULL,
        CancelledByUserId int NULL,
        CancellationReason nvarchar(500) NULL,
        EmailSentAt datetime2 NULL,
        CancellationEmailSentAt datetime2 NULL,
        RowVersion rowversion NOT NULL,
        CreatedAt datetime2 NOT NULL,
        CreatedBy nvarchar(200) NOT NULL,
        CreatedByWindows nvarchar(200) NOT NULL,
        ModifiedAt datetime2 NULL,
        ModifiedBy nvarchar(200) NULL,
        ModifiedByWindows nvarchar(200) NULL,
        CONSTRAINT FK_WarehouseRequisitions_ProductionWorkplaces FOREIGN KEY (ProductionWorkplaceId)
            REFERENCES dbo.ProductionWorkplaces(Id),
        CONSTRAINT FK_WarehouseRequisitions_OrderRecipientGroups FOREIGN KEY (OrderRecipientGroupId)
            REFERENCES dbo.OrderRecipientGroups(Id)
    );

    CREATE INDEX IX_WarehouseRequisitions_Status ON dbo.WarehouseRequisitions(Status);
    CREATE INDEX IX_WarehouseRequisitions_ProductionWorkplaceId ON dbo.WarehouseRequisitions(ProductionWorkplaceId);
    CREATE INDEX IX_WarehouseRequisitions_SubmittedAt ON dbo.WarehouseRequisitions(SubmittedAt);
END
GO

IF OBJECT_ID('dbo.WarehouseRequisitionItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarehouseRequisitionItems (
        Id int IDENTITY(1,1) PRIMARY KEY,
        WarehouseRequisitionId int NOT NULL,
        ArticleNumber nvarchar(100) NOT NULL,
        ArticleDescription nvarchar(500) NOT NULL,
        Unit nvarchar(20) NULL,
        QuantityRequested decimal(18,4) NOT NULL,
        QuantityPicked decimal(18,4) NULL,
        Position int NOT NULL,
        CreatedAt datetime2 NOT NULL,
        CreatedBy nvarchar(200) NOT NULL,
        CreatedByWindows nvarchar(200) NOT NULL,
        ModifiedAt datetime2 NULL,
        ModifiedBy nvarchar(200) NULL,
        ModifiedByWindows nvarchar(200) NULL,
        CONSTRAINT FK_WarehouseRequisitionItems_WarehouseRequisitions FOREIGN KEY (WarehouseRequisitionId)
            REFERENCES dbo.WarehouseRequisitions(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_WarehouseRequisitionItems_RequisitionId_Position
        ON dbo.WarehouseRequisitionItems(WarehouseRequisitionId, Position);
    CREATE UNIQUE INDEX IX_WarehouseRequisitionItems_RequisitionId_ArticleNumber
        ON dbo.WarehouseRequisitionItems(WarehouseRequisitionId, ArticleNumber);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId LIKE '%_AddWarehouseRequisitions')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('<MIGRATION_ID>_AddWarehouseRequisitions', '<PRODUCT_VERSION>');
END
GO
```

`<MIGRATION_ID>` mit dem Timestamp-Präfix aus Step 5 ersetzen, `<PRODUCT_VERSION>` via:

```bash
grep "ProductVersion" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Migrations/ApplicationDbContextModelSnapshot.cs" | head -1
```

- [ ] **Step 8: FreshInstall ergänzen**

In `SQL/00_FreshInstall.sql` an passender Stelle (nach den anderen `dbo.*Items`-Tabellen, vor der AppSettings-Seed-Sektion) die beiden CREATE-TABLE-Blöcke aus SQL/53 einfügen — ohne den `__EFMigrationsHistory`-Block (FreshInstall erzeugt die History bereits).

```bash
grep -n "CREATE TABLE \[dbo\]\.\[PartRequisitions\]" "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/SQL/00_FreshInstall.sql"
```

Direkt nach dem PartRequisitions-Block einfügen:

```sql
CREATE TABLE [dbo].[WarehouseRequisitions] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ProductionWorkplaceId] INT NOT NULL,
    [Status] TINYINT NOT NULL,
    [OrderRecipientGroupId] INT NULL,
    [SubmittedAt] DATETIME2 NULL,
    [SubmittedByUserId] INT NULL,
    [ClosedAt] DATETIME2 NULL,
    [ClosedByUserId] INT NULL,
    [CancelledAt] DATETIME2 NULL,
    [CancelledByUserId] INT NULL,
    [CancellationReason] NVARCHAR(500) NULL,
    [EmailSentAt] DATETIME2 NULL,
    [CancellationEmailSentAt] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    [CreatedBy] NVARCHAR(200) NOT NULL,
    [CreatedByWindows] NVARCHAR(200) NOT NULL,
    [ModifiedAt] DATETIME2 NULL,
    [ModifiedBy] NVARCHAR(200) NULL,
    [ModifiedByWindows] NVARCHAR(200) NULL,
    CONSTRAINT [FK_WarehouseRequisitions_ProductionWorkplaces] FOREIGN KEY ([ProductionWorkplaceId])
        REFERENCES [dbo].[ProductionWorkplaces]([Id]),
    CONSTRAINT [FK_WarehouseRequisitions_OrderRecipientGroups] FOREIGN KEY ([OrderRecipientGroupId])
        REFERENCES [dbo].[OrderRecipientGroups]([Id])
);
CREATE INDEX [IX_WarehouseRequisitions_Status] ON [dbo].[WarehouseRequisitions]([Status]);
CREATE INDEX [IX_WarehouseRequisitions_ProductionWorkplaceId] ON [dbo].[WarehouseRequisitions]([ProductionWorkplaceId]);
CREATE INDEX [IX_WarehouseRequisitions_SubmittedAt] ON [dbo].[WarehouseRequisitions]([SubmittedAt]);
GO

CREATE TABLE [dbo].[WarehouseRequisitionItems] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [WarehouseRequisitionId] INT NOT NULL,
    [ArticleNumber] NVARCHAR(100) NOT NULL,
    [ArticleDescription] NVARCHAR(500) NOT NULL,
    [Unit] NVARCHAR(20) NULL,
    [QuantityRequested] DECIMAL(18,4) NOT NULL,
    [QuantityPicked] DECIMAL(18,4) NULL,
    [Position] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    [CreatedBy] NVARCHAR(200) NOT NULL,
    [CreatedByWindows] NVARCHAR(200) NOT NULL,
    [ModifiedAt] DATETIME2 NULL,
    [ModifiedBy] NVARCHAR(200) NULL,
    [ModifiedByWindows] NVARCHAR(200) NULL,
    CONSTRAINT [FK_WarehouseRequisitionItems_WarehouseRequisitions] FOREIGN KEY ([WarehouseRequisitionId])
        REFERENCES [dbo].[WarehouseRequisitions]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_WarehouseRequisitionItems_RequisitionId_Position]
    ON [dbo].[WarehouseRequisitionItems]([WarehouseRequisitionId], [Position]);
CREATE UNIQUE INDEX [IX_WarehouseRequisitionItems_RequisitionId_ArticleNumber]
    ON [dbo].[WarehouseRequisitionItems]([WarehouseRequisitionId], [ArticleNumber]);
GO
```

- [ ] **Step 9: Build + Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Expected: 0 Fehler, alle Tests grün, kein Pending-Model-Changes-Warning.

- [ ] **Step 10: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Models/WarehouseRequisition.cs IdealAkeWms/Models/WarehouseRequisitionItem.cs IdealAkeWms/Models/WarehouseRequisitionStatus.cs IdealAkeWms/Data/ApplicationDbContext.cs IdealAkeWms/Migrations/ SQL/53_AddWarehouseRequisitions.sql SQL/00_FreshInstall.sql
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(warehouse): schema for WarehouseRequisition aggregate

- WarehouseRequisitionStatus enum (Draft=1/Submitted=2/Closed=3/Cancelled=4)
- WarehouseRequisition (Header) mit RowVersion, FK Workplace + RecipientGroup
- WarehouseRequisitionItem (Items) mit Unique-Index (Requisition, ArticleNumber)
- EF-Migration AddWarehouseRequisitions, idempotenter SQL/53, FreshInstall"
```

---

## Task 2: AppSettingKey + Seed (App + ServiceSetting)

**Files:**
- Modify: `IdealAkeWms/Models/AppSettingKeys.cs`
- Modify: `IdealAkeWms/Program.cs`
- Modify: `IDEALAKEWMSService/appsettings.json`
- Modify: `SQL/53_AddWarehouseRequisitions.sql` (zusätzlicher Seed)
- Modify: `SQL/00_FreshInstall.sql` (zusätzliche Zeilen)

- [ ] **Step 1: AppSettingKeys-Konstanten**

In `IdealAkeWms/Models/AppSettingKeys.cs` bei den Picking/Leitstand-Keys:

```csharp
    public const string DefaultLagerbestellempfaengerId = "DefaultLagerbestellempfaengerId";
```

- [ ] **Step 2: Program.cs Seed (App-Settings)**

In `IdealAkeWms/Program.cs` im passenden Tuple-Array (suche nach `BestellungenAktiv`) einen Eintrag ergänzen:

```csharp
("DefaultLagerbestellempfaengerId", "", "Default-OrderRecipientGroup-ID fuer Lagerbestellungen (leer = Submit blockt)"),
```

- [ ] **Step 3: Service-Settings-Seed (DB + appsettings.json)**

In `IDEALAKEWMSService/appsettings.json` im `Sync`-Section einen Eintrag ergänzen:

```json
"WarehouseRequisitionEmailEnabled": false
```

In `IdealAkeWms/Program.cs` im `serviceSettingSeed`-Tuple (analog `Sync:PartRequisitionEmailEnabled`):

```csharp
("Sync:WarehouseRequisitionEmailEnabled", "false", "Lagerbestellung", "Aktiviert E-Mail-Versand fuer Lagerbestellungen im SyncWorker"),
```

- [ ] **Step 4: SQL-Seeds in SQL/53 ergänzen**

Vor dem `__EFMigrationsHistory`-Block in `SQL/53_AddWarehouseRequisitions.sql`:

```sql
IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = 'DefaultLagerbestellempfaengerId')
    INSERT INTO dbo.AppSettings ([Key], [Value], [Description])
    VALUES ('DefaultLagerbestellempfaengerId', '', 'Default-OrderRecipientGroup-ID fuer Lagerbestellungen (leer = Submit blockt)');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = 'Sync:WarehouseRequisitionEmailEnabled')
    INSERT INTO dbo.ServiceSettings ([Key], [Value], [Category], [Description])
    VALUES ('Sync:WarehouseRequisitionEmailEnabled', 'false', 'Lagerbestellung', 'Aktiviert E-Mail-Versand fuer Lagerbestellungen im SyncWorker');
GO
```

- [ ] **Step 5: FreshInstall ergänzen**

Im AppSettings-INSERT-Block in `SQL/00_FreshInstall.sql` (suche nach `OseonReportingOverdueLookbackDays`):

```sql
    ('DefaultLagerbestellempfaengerId', '', 'Default-OrderRecipientGroup-ID fuer Lagerbestellungen (leer = Submit blockt)'),
```

Im ServiceSettings-INSERT-Block in `SQL/00_FreshInstall.sql` (suche nach `Sync:PartRequisitionEmailEnabled`):

```sql
    ('Sync:WarehouseRequisitionEmailEnabled', 'false', 'Lagerbestellung', 'Aktiviert E-Mail-Versand fuer Lagerbestellungen im SyncWorker'),
```

(Achtung Komma-Style — letzte Zeile vor Semikolon ohne Komma.)

- [ ] **Step 6: Build + Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

- [ ] **Step 7: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Models/AppSettingKeys.cs IdealAkeWms/Program.cs IDEALAKEWMSService/appsettings.json SQL/53_AddWarehouseRequisitions.sql SQL/00_FreshInstall.sql
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(warehouse): seed DefaultLagerbestellempfaengerId + email-toggle ServiceSetting"
```

---

## Task 3: `IProductionWorkplaceRepository.GetByUserIdAsync` (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IProductionWorkplaceRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/ProductionWorkplaceRepository.cs`
- Create: `IdealAkeWms.Tests/Repositories/ProductionWorkplaceRepositoryUserTests.cs`

- [ ] **Step 1: Test-Datei mit 3 Tests anlegen**

`IdealAkeWms.Tests/Repositories/ProductionWorkplaceRepositoryUserTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class ProductionWorkplaceRepositoryUserTests
{
    private static User NewUser(string name)
        => new() { Name = name, IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };

    private static ProductionWorkplace NewWorkplace(string name)
        => new() { Name = name, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };

    [Fact]
    public async Task GetByUserId_NoAssignment_ReturnsEmpty()
    {
        var ctx = TestDbContextFactory.Create();
        var u = NewUser("u1");
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();

        var repo = new ProductionWorkplaceRepository(ctx);
        var result = await repo.GetByUserIdAsync(u.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserId_OneAssignment_ReturnsOne()
    {
        var ctx = TestDbContextFactory.Create();
        var u = NewUser("u1"); var wp = NewWorkplace("WB-A");
        ctx.Users.Add(u); ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();
        ctx.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            UserId = u.Id, ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var repo = new ProductionWorkplaceRepository(ctx);
        var result = await repo.GetByUserIdAsync(u.Id);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("WB-A");
    }

    [Fact]
    public async Task GetByUserId_MultipleAssignments_ReturnsAlphabetical()
    {
        var ctx = TestDbContextFactory.Create();
        var u = NewUser("u1");
        var wpC = NewWorkplace("WB-C"); var wpA = NewWorkplace("WB-A"); var wpB = NewWorkplace("WB-B");
        ctx.Users.Add(u); ctx.ProductionWorkplaces.AddRange(wpC, wpA, wpB);
        await ctx.SaveChangesAsync();
        ctx.ProductionWorkplaceUsers.AddRange(
            new ProductionWorkplaceUser { UserId = u.Id, ProductionWorkplaceId = wpC.Id, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionWorkplaceUser { UserId = u.Id, ProductionWorkplaceId = wpA.Id, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionWorkplaceUser { UserId = u.Id, ProductionWorkplaceId = wpB.Id, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var repo = new ProductionWorkplaceRepository(ctx);
        var result = await repo.GetByUserIdAsync(u.Id);

        result.Select(w => w.Name).Should().ContainInOrder("WB-A", "WB-B", "WB-C");
    }
}
```

- [ ] **Step 2: Tests laufen → fail erwartet**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~ProductionWorkplaceRepositoryUserTests" 2>&1 | tail -10
```

Expected: Build error (`GetByUserIdAsync` not defined).

- [ ] **Step 3: Interface erweitern**

In `IdealAkeWms/Data/Repositories/IProductionWorkplaceRepository.cs` bei den anderen Methoden:

```csharp
    Task<List<ProductionWorkplace>> GetByUserIdAsync(int userId);
```

- [ ] **Step 4: Implementation**

In `IdealAkeWms/Data/Repositories/ProductionWorkplaceRepository.cs` ergänzen:

```csharp
    public async Task<List<ProductionWorkplace>> GetByUserIdAsync(int userId)
    {
        return await _context.ProductionWorkplaceUsers
            .AsNoTracking()
            .Where(wu => wu.UserId == userId)
            .Select(wu => wu.ProductionWorkplace)
            .OrderBy(w => w.Name)
            .ToListAsync();
    }
```

(`Microsoft.EntityFrameworkCore` und `IdealAkeWms.Models` müssen importiert sein — sind sie meist schon.)

- [ ] **Step 5: Tests grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~ProductionWorkplaceRepositoryUserTests" 2>&1 | tail -10
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

Expected: 3 neue Tests grün, full suite grün.

- [ ] **Step 6: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Data/Repositories/IProductionWorkplaceRepository.cs IdealAkeWms/Data/Repositories/ProductionWorkplaceRepository.cs IdealAkeWms.Tests/Repositories/ProductionWorkplaceRepositoryUserTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(workplace): GetByUserIdAsync repo method (alphabetisch)"
```

---

## Task 4: `IWarehouseRequisitionRepository` (TDD, Kern-CRUD + Lifecycle)

**Files:**
- Create: `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`
- Create: `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`
- Create: `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`
- Modify: `IdealAkeWms/Program.cs` (DI-Registrierung)

- [ ] **Step 1: Test-Datei mit 8 Tests**

`IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class WarehouseRequisitionRepositoryTests
{
    private static async Task<(int userId, int workplaceId, int recipientGroupId)> SeedAsync(ApplicationDbContext ctx)
    {
        var u = new User { Name = "tester", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var wp = new ProductionWorkplace { Name = "WB-1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var grp = new OrderRecipientGroup { Name = "Lager", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.Users.Add(u); ctx.ProductionWorkplaces.Add(wp); ctx.OrderRecipientGroups.Add(grp);
        await ctx.SaveChangesAsync();
        return (u.Id, wp.Id, grp.Id);
    }

    [Fact]
    public async Task CreateDraftAsync_SetsStatusAndAudit()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, _) = await SeedAsync(ctx);

        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "tester", "DOMAIN\\tester");

        var r = await ctx.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Draft);
        r.ProductionWorkplaceId.Should().Be(wpId);
        r.CreatedBy.Should().Be("tester");
        r.CreatedByWindows.Should().Be("DOMAIN\\tester");
    }

    [Fact]
    public async Task AddItem_AssignsPositionN_Plus1()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, _) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "t", "t");

        await repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 5m, "t", "t");
        await repo.AddItemAsync(id, "ART-2", "Mutter", "Stk", 10m, "t", "t");

        var items = ctx.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        items.Should().HaveCount(2);
        items[0].Position.Should().Be(1);
        items[1].Position.Should().Be(2);
    }

    [Fact]
    public async Task AddItem_DuplicateArticle_Throws()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, _) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "t", "t");

        await repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 5m, "t", "t");
        Func<Task> act = () => repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 3m, "t", "t");

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SubmitAsync_SetsStatusAndAudit()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, grpId) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "t", "t");
        await repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 5m, "t", "t");

        var r = await ctx.WarehouseRequisitions.FindAsync(id);
        await repo.SubmitAsync(id, grpId, userId, "t", "t", r!.RowVersion);

        var updated = await ctx.WarehouseRequisitions.FindAsync(id);
        updated!.Status.Should().Be(WarehouseRequisitionStatus.Submitted);
        updated.OrderRecipientGroupId.Should().Be(grpId);
        updated.SubmittedAt.Should().NotBeNull();
        updated.SubmittedByUserId.Should().Be(userId);
    }

    [Fact]
    public async Task CloseAsync_WritesItemQuantitiesPicked()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, grpId) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "t", "t");
        await repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 5m, "t", "t");
        await repo.AddItemAsync(id, "ART-2", "Mutter", "Stk", 10m, "t", "t");
        var rBefore = await ctx.WarehouseRequisitions.FindAsync(id);
        await repo.SubmitAsync(id, grpId, userId, "t", "t", rBefore!.RowVersion);

        var rSubmitted = await ctx.WarehouseRequisitions.FindAsync(id);
        var items = ctx.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();
        var quantities = new Dictionary<int, decimal>
        {
            [items[0].Id] = 4m,
            [items[1].Id] = 10m
        };

        await repo.CloseAsync(id, quantities, userId, "t", "t", rSubmitted!.RowVersion);

        var updated = await ctx.WarehouseRequisitions
            .Include(r => r.Items)
            .FirstAsync(r => r.Id == id);
        updated.Status.Should().Be(WarehouseRequisitionStatus.Closed);
        updated.Items.First(i => i.ArticleNumber == "ART-1").QuantityPicked.Should().Be(4m);
        updated.Items.First(i => i.ArticleNumber == "ART-2").QuantityPicked.Should().Be(10m);
    }

    [Fact]
    public async Task CancelAsync_SubmittedThenCancelled_Tracks()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, grpId) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "t", "t");
        await repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 5m, "t", "t");
        var r1 = await ctx.WarehouseRequisitions.FindAsync(id);
        await repo.SubmitAsync(id, grpId, userId, "t", "t", r1!.RowVersion);
        var r2 = await ctx.WarehouseRequisitions.FindAsync(id);

        await repo.CancelAsync(id, "Falsch erfasst", userId, "t", "t", r2!.RowVersion);

        var updated = await ctx.WarehouseRequisitions.FindAsync(id);
        updated!.Status.Should().Be(WarehouseRequisitionStatus.Cancelled);
        updated.CancellationReason.Should().Be("Falsch erfasst");
        updated.CancelledAt.Should().NotBeNull();
        updated.CancelledByUserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetForWarehouse_FiltersStatusNotDraft()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, grpId) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);

        // 1 Draft + 1 Submitted
        var draftId = await repo.CreateDraftAsync(wpId, userId, "t", "t");
        var submittedId = await repo.CreateDraftAsync(wpId, userId, "t", "t");
        await repo.AddItemAsync(submittedId, "ART-1", "Schraube", "Stk", 5m, "t", "t");
        var rs = await ctx.WarehouseRequisitions.FindAsync(submittedId);
        await repo.SubmitAsync(submittedId, grpId, userId, "t", "t", rs!.RowVersion);

        var (items, total) = await repo.GetForWarehouseAsync(statusFilter: null, workplaceId: null, page: 1, pageSize: 25);

        items.Select(i => i.Id).Should().NotContain(draftId);
        items.Select(i => i.Id).Should().Contain(submittedId);
        total.Should().Be(1);
    }

    [Fact]
    public async Task GetPendingSubmitEmails_FindsSubmittedWithoutEmail()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, grpId) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "t", "t");
        await repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 5m, "t", "t");
        var r = await ctx.WarehouseRequisitions.FindAsync(id);
        await repo.SubmitAsync(id, grpId, userId, "t", "t", r!.RowVersion);

        var pending = await repo.GetPendingSubmitEmailsAsync();

        pending.Should().HaveCount(1);
        pending[0].Id.Should().Be(id);
    }
}
```

- [ ] **Step 2: Tests laufen → fail**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~WarehouseRequisitionRepositoryTests" 2>&1 | tail -10
```

- [ ] **Step 3: Interface anlegen**

`IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IWarehouseRequisitionRepository
{
    Task<int> CreateDraftAsync(int productionWorkplaceId, int currentUserId, string currentUserName, string windowsUserName);
    Task<WarehouseRequisition?> GetByIdAsync(int id, bool includeItems = true);
    Task<List<WarehouseRequisition>> GetForUserAsync(int userId, int historyDays = 30);
    Task<(List<WarehouseRequisition> Items, int TotalCount)> GetForWarehouseAsync(
        WarehouseRequisitionStatus? statusFilter, int? workplaceId, int page, int pageSize);
    Task<List<WarehouseRequisition>> GetPendingSubmitEmailsAsync();
    Task<List<WarehouseRequisition>> GetPendingCancellationEmailsAsync();

    Task AddItemAsync(int requisitionId, string articleNumber, string description, string? unit,
        decimal quantity, string user, string winUser);
    Task UpdateItemQuantityAsync(int itemId, decimal quantity, string user, string winUser);
    Task RemoveItemAsync(int itemId);

    Task SubmitAsync(int id, int recipientGroupId, int submittedByUserId, string user, string winUser, byte[] rowVersion);
    Task CloseAsync(int id, IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
        int closedByUserId, string user, string winUser, byte[] rowVersion);
    Task CancelAsync(int id, string? reason, int cancelledByUserId, string user, string winUser, byte[] rowVersion);

    Task MarkEmailSentAsync(int id, DateTime sentAt);
    Task MarkCancellationEmailSentAsync(int id, DateTime sentAt);
}
```

- [ ] **Step 4: Implementation**

`IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`:

```csharp
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class WarehouseRequisitionRepository : IWarehouseRequisitionRepository
{
    private readonly ApplicationDbContext _context;
    public WarehouseRequisitionRepository(ApplicationDbContext context) { _context = context; }

    public async Task<int> CreateDraftAsync(int productionWorkplaceId, int currentUserId, string currentUserName, string windowsUserName)
    {
        var r = new WarehouseRequisition
        {
            ProductionWorkplaceId = productionWorkplaceId,
            Status = WarehouseRequisitionStatus.Draft,
            CreatedAt = DateTime.Now,
            CreatedBy = currentUserName,
            CreatedByWindows = windowsUserName
        };
        _context.WarehouseRequisitions.Add(r);
        await _context.SaveChangesAsync();
        return r.Id;
    }

    public async Task<WarehouseRequisition?> GetByIdAsync(int id, bool includeItems = true)
    {
        var q = _context.WarehouseRequisitions
            .Include(r => r.ProductionWorkplace)
            .Include(r => r.OrderRecipientGroup).ThenInclude(g => g!.Recipients)
            .AsQueryable();
        if (includeItems) q = q.Include(r => r.Items);
        return await q.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<WarehouseRequisition>> GetForUserAsync(int userId, int historyDays = 30)
    {
        var cutoff = DateTime.Now.AddDays(-historyDays);
        return await _context.WarehouseRequisitions
            .Include(r => r.ProductionWorkplace)
            .Include(r => r.Items)
            .Where(r => r.CreatedByUserId == userId
                ? true  // Placeholder — wir nutzen den Audit-User-Lookup ggf. via JOIN ueber CreatedBy?
                : false)
            .ToListAsync();
        // Hinweis: AuditableEntity hat nur CreatedBy (string). Wir filtern unten nach CreatedByUserId nicht direkt.
        // Daher: Filter via Service-Layer oder neue Spalte. Fuer MVP: zeige ALLE eigenen Listen via CreatedBy-Match.
    }

    // ... (siehe Folge-Steps fuer die restliche Impl)
```

**Wichtig:** `AuditableEntity` hat nur `CreatedBy` (string). Wir filtern `GetForUserAsync` nicht via UserId, sondern via `CreatedBy == userName`. Lese die exakte Property von `AuditableEntity`:

```bash
cat "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1/IdealAkeWms/Models/AuditableEntity.cs"
```

Falls dort `int? CreatedByUserId` existiert → benutzen. Sonst: Service-Layer übergibt den Username, Repository matcht string. Diese Einsicht gewinnt im laufenden Coden.

**Vollständige Implementation** der Methoden (in der Reihenfolge wie in Tests benötigt):

```csharp
    public async Task<List<WarehouseRequisition>> GetForUserAsync(int userId, int historyDays = 30)
    {
        var cutoff = DateTime.Now.AddDays(-historyDays);
        return await _context.WarehouseRequisitions
            .Include(r => r.ProductionWorkplace)
            .Include(r => r.Items)
            .Where(r =>
                r.Status == WarehouseRequisitionStatus.Draft  // Drafts immer
                || r.CreatedAt >= cutoff)                     // History last 30 days
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        // Filter "nur eigene" macht der Controller via CreatedBy == currentUser.GetDisplayName()
    }

    public async Task<(List<WarehouseRequisition> Items, int TotalCount)> GetForWarehouseAsync(
        WarehouseRequisitionStatus? statusFilter, int? workplaceId, int page, int pageSize)
    {
        var q = _context.WarehouseRequisitions
            .Include(r => r.ProductionWorkplace)
            .Include(r => r.Items)
            .Where(r => r.Status != WarehouseRequisitionStatus.Draft);
        if (statusFilter.HasValue) q = q.Where(r => r.Status == statusFilter.Value);
        if (workplaceId.HasValue) q = q.Where(r => r.ProductionWorkplaceId == workplaceId.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(r => r.SubmittedAt ?? r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<List<WarehouseRequisition>> GetPendingSubmitEmailsAsync()
    {
        return await _context.WarehouseRequisitions
            .Include(r => r.OrderRecipientGroup).ThenInclude(g => g!.Recipients)
            .Include(r => r.Items)
            .Include(r => r.ProductionWorkplace)
            .Where(r => r.Status == WarehouseRequisitionStatus.Submitted
                && r.EmailSentAt == null
                && r.OrderRecipientGroupId != null)
            .ToListAsync();
    }

    public async Task<List<WarehouseRequisition>> GetPendingCancellationEmailsAsync()
    {
        return await _context.WarehouseRequisitions
            .Include(r => r.OrderRecipientGroup).ThenInclude(g => g!.Recipients)
            .Include(r => r.Items)
            .Include(r => r.ProductionWorkplace)
            .Where(r => r.Status == WarehouseRequisitionStatus.Cancelled
                && r.EmailSentAt != null
                && r.CancellationEmailSentAt == null)
            .ToListAsync();
    }

    public async Task AddItemAsync(int requisitionId, string articleNumber, string description, string? unit,
        decimal quantity, string user, string winUser)
    {
        var nextPos = await _context.WarehouseRequisitionItems
            .Where(i => i.WarehouseRequisitionId == requisitionId)
            .Select(i => (int?)i.Position)
            .MaxAsync() ?? 0;

        _context.WarehouseRequisitionItems.Add(new WarehouseRequisitionItem
        {
            WarehouseRequisitionId = requisitionId,
            ArticleNumber = articleNumber,
            ArticleDescription = description,
            Unit = unit,
            QuantityRequested = quantity,
            Position = nextPos + 1,
            CreatedAt = DateTime.Now,
            CreatedBy = user,
            CreatedByWindows = winUser
        });
        var r = await _context.WarehouseRequisitions.FindAsync(requisitionId);
        if (r != null)
        {
            r.ModifiedAt = DateTime.Now;
            r.ModifiedBy = user;
            r.ModifiedByWindows = winUser;
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpdateItemQuantityAsync(int itemId, decimal quantity, string user, string winUser)
    {
        var item = await _context.WarehouseRequisitionItems.FindAsync(itemId);
        if (item == null) return;
        item.QuantityRequested = quantity;
        item.ModifiedAt = DateTime.Now;
        item.ModifiedBy = user;
        item.ModifiedByWindows = winUser;
        await _context.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(int itemId)
    {
        var item = await _context.WarehouseRequisitionItems.FindAsync(itemId);
        if (item == null) return;
        _context.WarehouseRequisitionItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    public async Task SubmitAsync(int id, int recipientGroupId, int submittedByUserId, string user, string winUser, byte[] rowVersion)
    {
        var r = await _context.WarehouseRequisitions.FindAsync(id)
            ?? throw new InvalidOperationException($"Requisition {id} not found");
        _context.Entry(r).Property(x => x.RowVersion).OriginalValue = rowVersion;
        r.Status = WarehouseRequisitionStatus.Submitted;
        r.OrderRecipientGroupId = recipientGroupId;
        r.SubmittedAt = DateTime.Now;
        r.SubmittedByUserId = submittedByUserId;
        r.ModifiedAt = DateTime.Now;
        r.ModifiedBy = user;
        r.ModifiedByWindows = winUser;
        await _context.SaveChangesAsync();
    }

    public async Task CloseAsync(int id, IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
        int closedByUserId, string user, string winUser, byte[] rowVersion)
    {
        var r = await _context.WarehouseRequisitions
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException($"Requisition {id} not found");
        _context.Entry(r).Property(x => x.RowVersion).OriginalValue = rowVersion;
        foreach (var item in r.Items)
        {
            item.QuantityPicked = itemQuantitiesPicked.TryGetValue(item.Id, out var q) ? q : item.QuantityRequested;
            item.ModifiedAt = DateTime.Now;
            item.ModifiedBy = user;
            item.ModifiedByWindows = winUser;
        }
        r.Status = WarehouseRequisitionStatus.Closed;
        r.ClosedAt = DateTime.Now;
        r.ClosedByUserId = closedByUserId;
        r.ModifiedAt = DateTime.Now;
        r.ModifiedBy = user;
        r.ModifiedByWindows = winUser;
        await _context.SaveChangesAsync();
    }

    public async Task CancelAsync(int id, string? reason, int cancelledByUserId, string user, string winUser, byte[] rowVersion)
    {
        var r = await _context.WarehouseRequisitions.FindAsync(id)
            ?? throw new InvalidOperationException($"Requisition {id} not found");
        _context.Entry(r).Property(x => x.RowVersion).OriginalValue = rowVersion;
        r.Status = WarehouseRequisitionStatus.Cancelled;
        r.CancellationReason = reason;
        r.CancelledAt = DateTime.Now;
        r.CancelledByUserId = cancelledByUserId;
        r.ModifiedAt = DateTime.Now;
        r.ModifiedBy = user;
        r.ModifiedByWindows = winUser;
        await _context.SaveChangesAsync();
    }

    public async Task MarkEmailSentAsync(int id, DateTime sentAt)
    {
        var r = await _context.WarehouseRequisitions.FindAsync(id);
        if (r == null) return;
        r.EmailSentAt = sentAt;
        await _context.SaveChangesAsync();
    }

    public async Task MarkCancellationEmailSentAsync(int id, DateTime sentAt)
    {
        var r = await _context.WarehouseRequisitions.FindAsync(id);
        if (r == null) return;
        r.CancellationEmailSentAt = sentAt;
        await _context.SaveChangesAsync();
    }
}
```

- [ ] **Step 5: DI-Registrierung in Program.cs**

In `IdealAkeWms/Program.cs` bei den anderen Repository-Registrierungen:

```csharp
builder.Services.AddScoped<IWarehouseRequisitionRepository, WarehouseRequisitionRepository>();
```

- [ ] **Step 6: Tests grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~WarehouseRequisitionRepositoryTests" 2>&1 | tail -10
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

- [ ] **Step 7: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs IdealAkeWms/Program.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(warehouse): IWarehouseRequisitionRepository CRUD + Lifecycle + Tests"
```

---

## Task 5: ViewModels

**Files (alle neu):**
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionListItemViewModel.cs`
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionListViewModel.cs`
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionEditItemViewModel.cs`
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionEditViewModel.cs`
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs`

- [ ] **Step 1: List-Item-VM**

`IdealAkeWms/Models/ViewModels/WarehouseRequisitionListItemViewModel.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public record WarehouseRequisitionListItemViewModel(
    int Id,
    string WorkplaceName,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    int ItemCount,
    WarehouseRequisitionStatus Status);
```

- [ ] **Step 2: List-VM (Container für beide Sichten)**

`IdealAkeWms/Models/ViewModels/WarehouseRequisitionListViewModel.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class WarehouseRequisitionListViewModel
{
    public List<WarehouseRequisitionListItemViewModel> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public WarehouseRequisitionStatus? StatusFilter { get; set; }
    public int? WorkplaceFilter { get; set; }
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public int OpenCount { get; set; }       // KPI fuer Lager-Sicht
}
```

- [ ] **Step 3: Edit-Item-VM**

`IdealAkeWms/Models/ViewModels/WarehouseRequisitionEditItemViewModel.cs`:

```csharp
namespace IdealAkeWms.Models.ViewModels;

public record WarehouseRequisitionEditItemViewModel(
    int Id,
    int Position,
    string ArticleNumber,
    string ArticleDescription,
    string? Unit,
    decimal QuantityRequested);
```

- [ ] **Step 4: Edit-VM**

`IdealAkeWms/Models/ViewModels/WarehouseRequisitionEditViewModel.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class WarehouseRequisitionEditViewModel
{
    public int Id { get; set; }
    public string WorkplaceName { get; set; } = string.Empty;
    public WarehouseRequisitionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<WarehouseRequisitionEditItemViewModel> Items { get; set; } = new();
}
```

- [ ] **Step 5: Detail-VM (Lager)**

`IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public record WarehouseRequisitionDetailItemViewModel(
    int Id,
    int Position,
    string ArticleNumber,
    string ArticleDescription,
    string? Unit,
    decimal QuantityRequested,
    decimal? QuantityPicked,
    string StorageLocations);

public class WarehouseRequisitionDetailViewModel
{
    public int Id { get; set; }
    public string WorkplaceName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public WarehouseRequisitionStatus Status { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<WarehouseRequisitionDetailItemViewModel> Items { get; set; } = new();
}
```

- [ ] **Step 6: Build**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
```

- [ ] **Step 7: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Models/ViewModels/WarehouseRequisition*.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(warehouse): view models for list/edit/detail"
```

---

## Task 6: `WarehouseRequisitionsController` (Erfasser, TDD)

**Files:**
- Create: `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs`
- Create: `IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs`

**Hinweis:** Zwei Action-Methoden für Werkbank-Resolution: `Index` zeigt eigene Listen + Werkbank-Auswahl-Form bei N≥2; `CreateDraft` POSTet die gewählte Werkbank-Id und legt Header an.

- [ ] **Step 1: Tests anlegen**

`IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class WarehouseRequisitionsControllerTests
{
    private static (WarehouseRequisitionsController ctrl, ApplicationDbContext ctx, int userId) Setup(int? defaultRecipientGroupId = null)
    {
        var ctx = TestDbContextFactory.Create();
        var u = new User { Name = "tester", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.Users.Add(u);
        ctx.SaveChanges();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(s => s.GetCurrentAppUserId()).Returns(u.Id);
        currentUser.Setup(s => s.GetDisplayName()).Returns("tester");
        currentUser.Setup(s => s.GetWindowsUserName()).Returns("DOMAIN\\tester");

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetIntValueAsync("DefaultLagerbestellempfaengerId", 0))
                .ReturnsAsync(defaultRecipientGroupId ?? 0);

        var workplaces = new ProductionWorkplaceRepository(ctx);
        var requisitions = new WarehouseRequisitionRepository(ctx);
        var groups = new OrderRecipientRepository(ctx);

        var ctrl = new WarehouseRequisitionsController(requisitions, workplaces, groups, currentUser.Object, settings.Object);
        ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        return (ctrl, ctx, u.Id);
    }

    [Fact]
    public async Task CreateDraft_NoWorkplaceAssigned_ReturnsErrorAndRedirects()
    {
        var (ctrl, _, _) = Setup();

        var result = await ctrl.CreateDraft(workplaceId: null) as RedirectToActionResult;

        result.Should().NotBeNull();
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task CreateDraft_OneWorkplace_AutoSelectsAndCreates()
    {
        var (ctrl, ctx, userId) = Setup();
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            UserId = userId, ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var result = await ctrl.CreateDraft(workplaceId: null) as RedirectToActionResult;

        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Edit");
        ctx.WarehouseRequisitions.Should().ContainSingle();
        ctx.WarehouseRequisitions.First().ProductionWorkplaceId.Should().Be(wp.Id);
    }

    [Fact]
    public async Task CreateDraft_TwoWorkplaces_NoSelection_RedirectsToIndexWithChoice()
    {
        var (ctrl, ctx, userId) = Setup();
        var wp1 = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var wp2 = new ProductionWorkplace { Name = "WB-B", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.AddRange(wp1, wp2);
        ctx.ProductionWorkplaceUsers.AddRange(
            new ProductionWorkplaceUser { UserId = userId, ProductionWorkplaceId = wp1.Id, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionWorkplaceUser { UserId = userId, ProductionWorkplaceId = wp2.Id, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var result = await ctrl.CreateDraft(workplaceId: null) as RedirectToActionResult;

        result!.ActionName.Should().Be("Index");
        ctx.WarehouseRequisitions.Should().BeEmpty("ohne Auswahl wird kein Draft angelegt");
    }

    [Fact]
    public async Task Submit_NoItems_RejectsWithWarning()
    {
        var (ctrl, ctx, userId) = Setup(defaultRecipientGroupId: 1);
        var grp = new OrderRecipientGroup { Id = 1, Name = "Lager", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.OrderRecipientGroups.Add(grp); ctx.ProductionWorkplaces.Add(wp);
        ctx.SaveChanges();
        var r = new WarehouseRequisition
        {
            ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        };
        ctx.WarehouseRequisitions.Add(r);
        await ctx.SaveChangesAsync();

        var result = await ctrl.Submit(r.Id) as RedirectToActionResult;

        result!.ActionName.Should().Be("Edit");
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
        ctx.WarehouseRequisitions.First().Status.Should().Be(WarehouseRequisitionStatus.Draft, "kein Submit ohne Items");
    }

    [Fact]
    public async Task Submit_NoDefaultRecipientSetting_RejectsWithWarning()
    {
        var (ctrl, ctx, userId) = Setup(defaultRecipientGroupId: 0);
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp); ctx.SaveChanges();
        var r = new WarehouseRequisition { ProductionWorkplaceId = wp.Id, CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester" };
        r.Items.Add(new WarehouseRequisitionItem
        {
            ArticleNumber = "ART-1", ArticleDescription = "x", QuantityRequested = 1, Position = 1,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        });
        ctx.WarehouseRequisitions.Add(r);
        await ctx.SaveChangesAsync();

        var result = await ctrl.Submit(r.Id) as RedirectToActionResult;

        result!.ActionName.Should().Be("Edit");
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
        ctx.WarehouseRequisitions.First().Status.Should().Be(WarehouseRequisitionStatus.Draft);
    }
}
```

- [ ] **Step 2: Tests laufen → fail (Build-Error WarehouseRequisitionsController not found)**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~WarehouseRequisitionsControllerTests" 2>&1 | tail -10
```

- [ ] **Step 3: Controller anlegen**

`IdealAkeWms/Controllers/WarehouseRequisitionsController.cs`:

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequirePickingOrStockAccess]
public class WarehouseRequisitionsController : Controller
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly IOrderRecipientRepository _groups;
    private readonly ICurrentUserService _user;
    private readonly IAppSettingRepository _settings;

    public WarehouseRequisitionsController(
        IWarehouseRequisitionRepository repo,
        IProductionWorkplaceRepository workplaces,
        IOrderRecipientRepository groups,
        ICurrentUserService user,
        IAppSettingRepository settings)
    {
        _repo = repo; _workplaces = workplaces; _groups = groups; _user = user; _settings = settings;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _user.GetCurrentAppUserId() ?? 0;
        var displayName = _user.GetDisplayName();
        var all = await _repo.GetForUserAsync(userId);
        var ownOnly = all.Where(r => r.CreatedBy == displayName).ToList();
        var vm = new WarehouseRequisitionListViewModel
        {
            Items = ownOnly.Select(r => new WarehouseRequisitionListItemViewModel(
                r.Id,
                r.ProductionWorkplace?.Name ?? "",
                r.CreatedBy,
                r.CreatedAt,
                r.SubmittedAt,
                r.Items.Count,
                r.Status)).ToList(),
            AvailableWorkplaces = await _workplaces.GetByUserIdAsync(userId)
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDraft(int? workplaceId)
    {
        var userId = _user.GetCurrentAppUserId() ?? 0;
        var workplaces = await _workplaces.GetByUserIdAsync(userId);

        if (workplaces.Count == 0)
        {
            TempData["WarningMessage"] = "Bitte Werkbank-Zuordnung in Stammdaten pflegen.";
            return RedirectToAction(nameof(Index));
        }

        int chosenWp;
        if (workplaceId.HasValue && workplaces.Any(w => w.Id == workplaceId.Value))
        {
            chosenWp = workplaceId.Value;
        }
        else if (workplaces.Count == 1)
        {
            chosenWp = workplaces[0].Id;
        }
        else
        {
            TempData["WarningMessage"] = "Bitte Werkbank waehlen.";
            return RedirectToAction(nameof(Index));
        }

        var newId = await _repo.CreateDraftAsync(chosenWp, userId, _user.GetDisplayName(), _user.GetWindowsUserName());
        return RedirectToAction(nameof(Edit), new { id = newId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null) return NotFound();
        var displayName = _user.GetDisplayName();
        if (r.CreatedBy != displayName)
            return Forbid();

        var vm = new WarehouseRequisitionEditViewModel
        {
            Id = r.Id,
            WorkplaceName = r.ProductionWorkplace?.Name ?? "",
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            RowVersion = r.RowVersion,
            Items = r.Items.OrderBy(i => i.Position).Select(i =>
                new WarehouseRequisitionEditItemViewModel(i.Id, i.Position, i.ArticleNumber, i.ArticleDescription, i.Unit, i.QuantityRequested)).ToList()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null) return NotFound();
        if (r.Status != WarehouseRequisitionStatus.Draft)
        {
            TempData["WarningMessage"] = "Nur Entwurfe koennen abgeschickt werden.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        if (r.Items.Count == 0)
        {
            TempData["WarningMessage"] = "Bitte mindestens einen Artikel hinzufuegen.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        var groupId = await _settings.GetIntValueAsync("DefaultLagerbestellempfaengerId", 0);
        if (groupId <= 0)
        {
            TempData["WarningMessage"] = "Default-Lagerbestellempfaenger nicht konfiguriert (Einstellungen).";
            return RedirectToAction(nameof(Edit), new { id });
        }
        var grp = await _groups.GetGroupByIdAsync(groupId);
        if (grp == null)
        {
            TempData["WarningMessage"] = "Konfigurierte Empfaenger-Gruppe nicht gefunden.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        await _repo.SubmitAsync(id, groupId, _user.GetCurrentAppUserId() ?? 0,
            _user.GetDisplayName(), _user.GetWindowsUserName(), r.RowVersion);

        TempData["SuccessMessage"] = $"Liste #{id} abgeschickt — wird per E-Mail gesendet (max. 15 Min).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null) return NotFound();
        if (r.Status != WarehouseRequisitionStatus.Draft && r.Status != WarehouseRequisitionStatus.Submitted)
        {
            TempData["WarningMessage"] = "Liste kann in diesem Status nicht storniert werden.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        await _repo.CancelAsync(id, reason, _user.GetCurrentAppUserId() ?? 0,
            _user.GetDisplayName(), _user.GetWindowsUserName(), r.RowVersion);
        TempData["SuccessMessage"] = $"Liste #{id} storniert.";
        return RedirectToAction(nameof(Index));
    }
}
```

- [ ] **Step 4: Tests grün**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --filter "FullyQualifiedName~WarehouseRequisitionsControllerTests" 2>&1 | tail -10
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

- [ ] **Step 5: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Controllers/WarehouseRequisitionsController.cs IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(warehouse): WarehouseRequisitionsController (Erfasser, Werkbank-Resolution, Submit/Cancel)"
```

---

## Task 7: `WarehousePickingController` (Lager, TDD)

**Files:**
- Create: `IdealAkeWms/Controllers/WarehousePickingController.cs`
- Create: `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs`

- [ ] **Step 1: Tests anlegen**

`IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class WarehousePickingControllerTests
{
    private static (WarehousePickingController ctrl, ApplicationDbContext ctx, int userId) Setup()
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
        stock.Setup(s => s.GetCurrentStockAsync(It.IsAny<string>()))
             .ReturnsAsync(new List<(string Code, decimal Quantity)>());

        var ctrl = new WarehousePickingController(repo, workplaces, stock.Object, current.Object);
        ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        return (ctrl, ctx, u.Id);
    }

    [Fact]
    public async Task Index_ShowsOnlyNonDraft()
    {
        var (ctrl, ctx, userId) = Setup();
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp); ctx.SaveChanges();

        var draft = new WarehouseRequisition { ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.Draft, CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" };
        var submitted = new WarehouseRequisition { ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.Submitted, SubmittedAt = DateTime.Now, CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" };
        ctx.WarehouseRequisitions.AddRange(draft, submitted);
        await ctx.SaveChangesAsync();

        var result = await ctrl.Index(statusFilter: null, workplaceId: null, page: 1) as ViewResult;

        var vm = result!.Model as WarehouseRequisitionListViewModel;
        vm!.Items.Should().HaveCount(1);
        vm.Items[0].Status.Should().Be(WarehouseRequisitionStatus.Submitted);
    }

    [Fact]
    public async Task Close_WritesItemQuantitiesPickedAndSetsStatus()
    {
        var (ctrl, ctx, userId) = Setup();
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp); ctx.SaveChanges();
        var r = new WarehouseRequisition
        {
            ProductionWorkplaceId = wp.Id,
            Status = WarehouseRequisitionStatus.Submitted,
            SubmittedAt = DateTime.Now,
            CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x"
        };
        r.Items.Add(new WarehouseRequisitionItem
        {
            ArticleNumber = "ART-1", ArticleDescription = "x", QuantityRequested = 5, Position = 1,
            CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x"
        });
        ctx.WarehouseRequisitions.Add(r);
        await ctx.SaveChangesAsync();
        var item = ctx.WarehouseRequisitionItems.First();

        var result = await ctrl.Close(r.Id, new[] { item.Id }, new[] { 4m }, r.RowVersion) as RedirectToActionResult;

        result.Should().NotBeNull();
        var updated = ctx.WarehouseRequisitions.Include(x => x.Items).First(x => x.Id == r.Id);
        updated.Status.Should().Be(WarehouseRequisitionStatus.Closed);
        updated.Items.First().QuantityPicked.Should().Be(4m);
    }
}
```

- [ ] **Step 2: Tests laufen → fail**

- [ ] **Step 3: Controller anlegen**

`IdealAkeWms/Controllers/WarehousePickingController.cs`:

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireStockAccess]
public class WarehousePickingController : Controller
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly IStockMovementRepository _stock;
    private readonly ICurrentUserService _user;

    public WarehousePickingController(
        IWarehouseRequisitionRepository repo,
        IProductionWorkplaceRepository workplaces,
        IStockMovementRepository stock,
        ICurrentUserService user)
    {
        _repo = repo; _workplaces = workplaces; _stock = stock; _user = user;
    }

    public async Task<IActionResult> Index(WarehouseRequisitionStatus? statusFilter, int? workplaceId, int page = 1)
    {
        const int pageSize = 25;
        var effectiveFilter = statusFilter ?? WarehouseRequisitionStatus.Submitted;
        var (items, total) = await _repo.GetForWarehouseAsync(effectiveFilter, workplaceId, page, pageSize);
        var allWorkplaces = await _workplaces.GetAllAsync();
        var openCount = (await _repo.GetForWarehouseAsync(WarehouseRequisitionStatus.Submitted, null, 1, 1)).TotalCount;

        var vm = new WarehouseRequisitionListViewModel
        {
            Items = items.Select(r => new WarehouseRequisitionListItemViewModel(
                r.Id, r.ProductionWorkplace?.Name ?? "", r.CreatedBy, r.CreatedAt,
                r.SubmittedAt, r.Items.Count, r.Status)).ToList(),
            TotalCount = total,
            CurrentPage = page,
            PageSize = pageSize,
            StatusFilter = statusFilter,
            WorkplaceFilter = workplaceId,
            AvailableWorkplaces = allWorkplaces.OrderBy(w => w.Name).ToList(),
            OpenCount = openCount
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null || r.Status == WarehouseRequisitionStatus.Draft) return NotFound();

        var detailItems = new List<WarehouseRequisitionDetailItemViewModel>();
        foreach (var i in r.Items.OrderBy(x => x.Position))
        {
            var stock = await _stock.GetCurrentStockAsync(i.ArticleNumber);
            var locationStr = string.Join(", ", stock.Where(s => s.Quantity > 0).Select(s => $"{s.Code} ({s.Quantity:N3})"));
            detailItems.Add(new WarehouseRequisitionDetailItemViewModel(
                i.Id, i.Position, i.ArticleNumber, i.ArticleDescription, i.Unit,
                i.QuantityRequested, i.QuantityPicked, locationStr));
        }

        var vm = new WarehouseRequisitionDetailViewModel
        {
            Id = r.Id,
            WorkplaceName = r.ProductionWorkplace?.Name ?? "",
            CreatedBy = r.CreatedBy,
            SubmittedAt = r.SubmittedAt,
            ClosedAt = r.ClosedAt,
            CancelledAt = r.CancelledAt,
            CancellationReason = r.CancellationReason,
            Status = r.Status,
            RowVersion = r.RowVersion,
            Items = detailItems
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id, int[] itemIds, decimal[] quantitiesPicked, byte[] rowVersion)
    {
        var dict = new Dictionary<int, decimal>();
        for (int idx = 0; idx < itemIds.Length; idx++)
            dict[itemIds[idx]] = idx < quantitiesPicked.Length ? quantitiesPicked[idx] : 0m;

        await _repo.CloseAsync(id, dict, _user.GetCurrentAppUserId() ?? 0,
            _user.GetDisplayName(), _user.GetWindowsUserName(), rowVersion);
        TempData["SuccessMessage"] = $"Liste #{id} abgeschlossen.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason, byte[] rowVersion)
    {
        await _repo.CancelAsync(id, reason, _user.GetCurrentAppUserId() ?? 0,
            _user.GetDisplayName(), _user.GetWindowsUserName(), rowVersion);
        TempData["SuccessMessage"] = $"Liste #{id} storniert.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Print(int id)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null || r.Status == WarehouseRequisitionStatus.Draft) return NotFound();

        var detailItems = new List<WarehouseRequisitionDetailItemViewModel>();
        foreach (var i in r.Items.OrderBy(x => x.Position))
        {
            var stock = await _stock.GetCurrentStockAsync(i.ArticleNumber);
            var locationStr = string.Join(", ", stock.Where(s => s.Quantity > 0).Select(s => $"{s.Code} ({s.Quantity:N3})"));
            detailItems.Add(new WarehouseRequisitionDetailItemViewModel(
                i.Id, i.Position, i.ArticleNumber, i.ArticleDescription, i.Unit,
                i.QuantityRequested, i.QuantityPicked, locationStr));
        }
        var vm = new WarehouseRequisitionDetailViewModel
        {
            Id = r.Id,
            WorkplaceName = r.ProductionWorkplace?.Name ?? "",
            CreatedBy = r.CreatedBy,
            SubmittedAt = r.SubmittedAt,
            Status = r.Status,
            Items = detailItems
        };
        return View(vm);  // Print.cshtml mit Layout=null
    }
}
```

- [ ] **Step 4: Tests grün, Commit**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Controllers/WarehousePickingController.cs IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(warehouse): WarehousePickingController (Lager: Index, Details, Close, Cancel, Print)"
```

---

## Task 8: `WarehouseRequisitionsApiController` (Items-CRUD JSON)

**Files:**
- Create: `IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs`

- [ ] **Step 1: API-Controller anlegen**

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Controllers.Api;

[ApiController]
[Route("api/warehouserequisitions")]
[RequirePickingOrStockAccess]
public class WarehouseRequisitionsApiController : ControllerBase
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IArticleRepository _articles;
    private readonly IStockMovementRepository _stock;
    private readonly ICurrentUserService _user;

    public WarehouseRequisitionsApiController(
        IWarehouseRequisitionRepository repo, IArticleRepository articles,
        IStockMovementRepository stock, ICurrentUserService user)
    {
        _repo = repo; _articles = articles; _stock = stock; _user = user;
    }

    public record AddItemRequest(string ArticleNumber, decimal Quantity);
    public record UpdateItemRequest(decimal Quantity);

    [HttpPost("{id:int}/items")]
    public async Task<IActionResult> AddItem(int id, [FromBody] AddItemRequest body)
    {
        var article = await _articles.GetByArticleNumberAsync(body.ArticleNumber);
        if (article == null) return BadRequest(new { error = "Artikel nicht gefunden." });
        try
        {
            await _repo.AddItemAsync(id, body.ArticleNumber, article.Description ?? "", article.Unit,
                body.Quantity, _user.GetDisplayName(), _user.GetWindowsUserName());
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { error = "Artikel ist bereits in der Liste." });
        }
        return Ok();
    }

    [HttpPut("items/{itemId:int}")]
    public async Task<IActionResult> UpdateItem(int itemId, [FromBody] UpdateItemRequest body)
    {
        await _repo.UpdateItemQuantityAsync(itemId, body.Quantity, _user.GetDisplayName(), _user.GetWindowsUserName());
        return Ok();
    }

    [HttpDelete("items/{itemId:int}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        await _repo.RemoveItemAsync(itemId);
        return Ok();
    }

    [HttpGet("stock")]
    public async Task<IActionResult> Stock([FromQuery] string articleNumber)
    {
        var stock = await _stock.GetCurrentStockAsync(articleNumber);
        var locationStr = string.Join(", ", stock.Where(s => s.Quantity > 0).Select(s => $"{s.Code} ({s.Quantity:N3})"));
        return Ok(new { locations = locationStr });
    }
}
```

**Hinweis:** `IArticleRepository.GetByArticleNumberAsync` existiert (siehe Exploration).

- [ ] **Step 2: Build + Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

- [ ] **Step 3: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(warehouse): API controller for items CRUD + stock lookup"
```

---

## Task 9: Erfasser-Views (Index + Edit)

**Files:**
- Create: `IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml`
- Create: `IdealAkeWms/Views/WarehouseRequisitions/Edit.cshtml`

- [ ] **Step 1: Index.cshtml**

```html
@model IdealAkeWms.Models.ViewModels.WarehouseRequisitionListViewModel
@{
    ViewData["Title"] = "Lagerbestellungen — meine Listen";
}

<div class="d-flex justify-content-between align-items-center flex-wrap gap-2 page-header">
    <h2 class="mb-0">Lagerbestellungen — meine Listen</h2>
    <form method="post" asp-action="CreateDraft" class="d-flex gap-2 align-items-center">
        @Html.AntiForgeryToken()
        @if (Model.AvailableWorkplaces.Count > 1)
        {
            <select name="workplaceId" class="form-select form-select-sm" required>
                <option value="">— Werkbank waehlen —</option>
                @foreach (var wp in Model.AvailableWorkplaces)
                {
                    <option value="@wp.Id">@wp.Name</option>
                }
            </select>
        }
        <button type="submit" class="btn btn-sm btn-primary">+ Neue Liste</button>
    </form>
</div>

@if (Model.Items.Count == 0)
{
    <div class="alert alert-info">Noch keine Listen erfasst.</div>
}
else
{
    <table class="table table-sm filterable-table">
        <thead>
            <tr>
                <th data-col-key="id">#</th>
                <th data-col-key="workplace">Werkbank</th>
                <th data-col-key="items">Pos</th>
                <th data-col-key="status">Status</th>
                <th data-col-key="created">Erstellt</th>
                <th data-col-key="submitted">Submit</th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var i in Model.Items)
            {
                <tr>
                    <td>@i.Id</td>
                    <td>@i.WorkplaceName</td>
                    <td>@i.ItemCount</td>
                    <td>
                        @switch (i.Status)
                        {
                            case IdealAkeWms.Models.WarehouseRequisitionStatus.Draft: <span class="badge bg-secondary">Entwurf</span> break;
                            case IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted: <span class="badge bg-primary">Abgeschickt</span> break;
                            case IdealAkeWms.Models.WarehouseRequisitionStatus.Closed: <span class="badge bg-success">Erledigt</span> break;
                            case IdealAkeWms.Models.WarehouseRequisitionStatus.Cancelled: <span class="badge bg-dark">Storniert</span> break;
                        }
                    </td>
                    <td>@i.CreatedAt.ToString("dd.MM.yyyy HH:mm")</td>
                    <td>@(i.SubmittedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—")</td>
                    <td><a asp-action="Edit" asp-route-id="@i.Id" class="btn btn-sm btn-outline-primary">@(i.Status == IdealAkeWms.Models.WarehouseRequisitionStatus.Draft ? "Bearbeiten" : "Ansehen")</a></td>
                </tr>
            }
        </tbody>
    </table>
}
```

- [ ] **Step 2: Edit.cshtml**

```html
@model IdealAkeWms.Models.ViewModels.WarehouseRequisitionEditViewModel
@{
    ViewData["Title"] = $"Lagerbestellung #{Model.Id}";
    bool isDraft = Model.Status == IdealAkeWms.Models.WarehouseRequisitionStatus.Draft;
}

<div class="d-flex justify-content-between align-items-center flex-wrap gap-2 page-header">
    <h2 class="mb-0">Lagerbestellung #@Model.Id</h2>
    <a asp-action="Index" class="btn btn-sm btn-outline-secondary">Zurueck</a>
</div>

<div class="card mb-3">
    <div class="card-body p-2">
        <strong>Werkbank:</strong> @Model.WorkplaceName
        <span class="ms-3"><strong>Status:</strong>
            @switch (Model.Status)
            {
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Draft: <span class="badge bg-secondary">Entwurf</span> break;
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted: <span class="badge bg-primary">Abgeschickt</span> break;
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Closed: <span class="badge bg-success">Erledigt</span> break;
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Cancelled: <span class="badge bg-dark">Storniert</span> break;
            }
        </span>
        <span class="ms-3"><strong>Erstellt:</strong> @Model.CreatedAt.ToString("dd.MM.yyyy HH:mm")</span>
    </div>
</div>

<table class="table table-sm" id="items-table">
    <thead>
        <tr>
            <th>Pos</th><th>Artikel-Nr</th><th>Bezeichnung</th><th>Menge</th><th>ME</th><th>Lagerplatz</th><th></th>
        </tr>
    </thead>
    <tbody>
        @foreach (var i in Model.Items)
        {
            <tr data-item-id="@i.Id">
                <td>@i.Position</td>
                <td>@i.ArticleNumber</td>
                <td>@i.ArticleDescription</td>
                <td>
                    @if (isDraft)
                    {
                        <input type="number" step="0.01" value="@i.QuantityRequested" class="form-control form-control-sm qty-edit" data-item-id="@i.Id" style="width:80px;" />
                    }
                    else
                    {
                        @i.QuantityRequested
                    }
                </td>
                <td>@i.Unit</td>
                <td class="stock-cell"><span class="text-muted small">…</span></td>
                <td>
                    @if (isDraft)
                    {
                        <button type="button" class="btn btn-sm btn-outline-danger remove-item" data-item-id="@i.Id">×</button>
                    }
                </td>
            </tr>
        }
    </tbody>
</table>

@if (isDraft)
{
    <div class="card mb-3">
        <div class="card-body">
            <label class="form-label">Artikel hinzufuegen</label>
            <input id="article-search" type="text" class="form-control" placeholder="Artikel-Nr oder Bezeichnung" autocomplete="off" />
            <div id="article-search-results" class="list-group mt-1" style="max-height: 300px; overflow-y: auto;"></div>
        </div>
    </div>

    <form method="post" asp-action="Submit" asp-route-id="@Model.Id" class="d-inline">
        @Html.AntiForgeryToken()
        <button type="submit" class="btn btn-primary" @(Model.Items.Count == 0 ? "disabled" : "")>Abschicken</button>
    </form>
}

@if (Model.Status == IdealAkeWms.Models.WarehouseRequisitionStatus.Draft || Model.Status == IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted)
{
    <button type="button" class="btn btn-outline-danger ms-2" data-bs-toggle="modal" data-bs-target="#cancel-modal">Stornieren</button>

    <div class="modal fade" id="cancel-modal" tabindex="-1">
        <div class="modal-dialog">
            <form method="post" asp-action="Cancel" asp-route-id="@Model.Id">
                @Html.AntiForgeryToken()
                <div class="modal-content">
                    <div class="modal-header"><h5 class="modal-title">Liste stornieren</h5></div>
                    <div class="modal-body">
                        <label class="form-label">Grund (optional)</label>
                        <textarea name="reason" class="form-control" rows="3"></textarea>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Abbrechen</button>
                        <button type="submit" class="btn btn-danger">Stornieren</button>
                    </div>
                </div>
            </form>
        </div>
    </div>
}

@section Scripts {
<script>
(function() {
    const reqId = @Model.Id;

    function loadStock(row) {
        const articleNumber = row.cells[1].innerText.trim();
        if (!articleNumber) return;
        fetch(`/api/warehouserequisitions/stock?articleNumber=${encodeURIComponent(articleNumber)}`)
            .then(r => r.json())
            .then(data => {
                const cell = row.querySelector('.stock-cell');
                if (cell) cell.innerHTML = `<small class="text-muted">${data.locations || '—'}</small>`;
            });
    }
    document.querySelectorAll('#items-table tbody tr').forEach(loadStock);

    @if (isDraft)
    {
        <text>
        const search = document.getElementById('article-search');
        const results = document.getElementById('article-search-results');
        let timer;
        search.addEventListener('input', () => {
            clearTimeout(timer);
            timer = setTimeout(() => {
                const q = search.value.trim();
                if (q.length < 2) { results.innerHTML = ''; return; }
                fetch(`/api/articles/search?q=${encodeURIComponent(q)}&limit=20`)
                    .then(r => r.json())
                    .then(arr => {
                        results.innerHTML = arr.map(a => `<button type="button" class="list-group-item list-group-item-action" data-art="${a.text.split(' - ')[0]}">${a.text}</button>`).join('');
                    });
            }, 250);
        });
        results.addEventListener('click', e => {
            const btn = e.target.closest('button');
            if (!btn) return;
            const articleNumber = btn.dataset.art;
            fetch(`/api/warehouserequisitions/${reqId}/items`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ articleNumber, quantity: 1 })
            }).then(r => r.ok ? location.reload() : r.json().then(j => alert(j.error || 'Fehler')));
        });

        document.querySelectorAll('.qty-edit').forEach(inp => {
            inp.addEventListener('change', () => {
                fetch(`/api/warehouserequisitions/items/${inp.dataset.itemId}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ quantity: parseFloat(inp.value) || 0 })
                });
            });
        });
        document.querySelectorAll('.remove-item').forEach(btn => {
            btn.addEventListener('click', () => {
                if (!confirm('Artikel entfernen?')) return;
                fetch(`/api/warehouserequisitions/items/${btn.dataset.itemId}`, { method: 'DELETE' })
                    .then(() => location.reload());
            });
        });
        </text>
    }
})();
</script>
}
```

- [ ] **Step 3: Build + Razor-Compile**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
```

- [ ] **Step 4: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Views/WarehouseRequisitions/
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(warehouse): Erfasser-Views (Index + Edit mit Article-Search)"
```

---

## Task 10: Lager-Views (Index + Details + Print)

**Files:**
- Create: `IdealAkeWms/Views/WarehousePicking/Index.cshtml`
- Create: `IdealAkeWms/Views/WarehousePicking/Details.cshtml`
- Create: `IdealAkeWms/Views/WarehousePicking/Print.cshtml`

- [ ] **Step 1: Index.cshtml** (Lager-Liste)

```html
@model IdealAkeWms.Models.ViewModels.WarehouseRequisitionListViewModel
@{
    ViewData["Title"] = "Lagerbestellungen — Lager";
}

<div class="d-flex justify-content-between align-items-center flex-wrap gap-2 page-header">
    <h2 class="mb-0">Lagerbestellungen — Lager</h2>
    <span class="badge bg-primary fs-6">Offen: @Model.OpenCount</span>
</div>

<form method="get" asp-action="Index" class="card card-body mb-3">
    <div class="row g-2 align-items-end">
        <div class="col-md-3">
            <label class="form-label">Status</label>
            <select name="statusFilter" class="form-select form-select-sm">
                <option value="">— Alle (ausser Entwurf) —</option>
                <option value="Submitted" selected="@(Model.StatusFilter == IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted)">Abgeschickt</option>
                <option value="Closed" selected="@(Model.StatusFilter == IdealAkeWms.Models.WarehouseRequisitionStatus.Closed)">Erledigt</option>
                <option value="Cancelled" selected="@(Model.StatusFilter == IdealAkeWms.Models.WarehouseRequisitionStatus.Cancelled)">Storniert</option>
            </select>
        </div>
        <div class="col-md-3">
            <label class="form-label">Werkbank</label>
            <select name="workplaceId" class="form-select form-select-sm">
                <option value="">— alle —</option>
                @foreach (var wp in Model.AvailableWorkplaces)
                {
                    <option value="@wp.Id" selected="@(Model.WorkplaceFilter == wp.Id)">@wp.Name</option>
                }
            </select>
        </div>
        <div class="col-auto"><button type="submit" class="btn btn-sm btn-primary">Filtern</button></div>
        <div class="col-auto"><a asp-action="Index" class="btn btn-sm btn-outline-secondary">Reset</a></div>
    </div>
</form>

@if (Model.Items.Count == 0)
{
    <div class="alert alert-info">Keine Listen gefunden.</div>
}
else
{
    <table class="table table-sm filterable-table">
        <thead>
            <tr>
                <th data-col-key="id">#</th>
                <th data-col-key="workplace">Werkbank</th>
                <th data-col-key="creator">Erfasser</th>
                <th data-col-key="submitted">Submit</th>
                <th data-col-key="items">Pos</th>
                <th data-col-key="status">Status</th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var i in Model.Items)
            {
                <tr>
                    <td>@i.Id</td>
                    <td>@i.WorkplaceName</td>
                    <td>@i.CreatedBy</td>
                    <td>@(i.SubmittedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—")</td>
                    <td>@i.ItemCount</td>
                    <td>
                        @switch (i.Status)
                        {
                            case IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted: <span class="badge bg-primary">Abgeschickt</span> break;
                            case IdealAkeWms.Models.WarehouseRequisitionStatus.Closed: <span class="badge bg-success">Erledigt</span> break;
                            case IdealAkeWms.Models.WarehouseRequisitionStatus.Cancelled: <span class="badge bg-dark">Storniert</span> break;
                        }
                    </td>
                    <td><a asp-action="Details" asp-route-id="@i.Id" class="btn btn-sm btn-outline-primary">Detail</a></td>
                </tr>
            }
        </tbody>
    </table>
}
```

- [ ] **Step 2: Details.cshtml** (Lager-Detail mit Close/Cancel/Print-Buttons)

```html
@model IdealAkeWms.Models.ViewModels.WarehouseRequisitionDetailViewModel
@{
    ViewData["Title"] = $"Lagerbestellung #{Model.Id}";
    bool isSubmitted = Model.Status == IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted;
}

<div class="d-flex justify-content-between align-items-center flex-wrap gap-2 page-header">
    <h2 class="mb-0">Lagerbestellung #@Model.Id</h2>
    <a asp-action="Index" class="btn btn-sm btn-outline-secondary">Zurueck</a>
</div>

<div class="card mb-3">
    <div class="card-body p-2">
        <div><strong>Werkbank:</strong> @Model.WorkplaceName</div>
        <div><strong>Erfasser:</strong> @Model.CreatedBy</div>
        <div><strong>Submit:</strong> @(Model.SubmittedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—")</div>
        <div><strong>Status:</strong>
            @switch (Model.Status)
            {
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted: <span class="badge bg-primary">Abgeschickt</span> break;
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Closed: <span class="badge bg-success">Erledigt am @Model.ClosedAt?.ToString("dd.MM.yyyy HH:mm")</span> break;
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Cancelled: <span class="badge bg-dark">Storniert am @Model.CancelledAt?.ToString("dd.MM.yyyy HH:mm")</span> break;
            }
        </div>
        @if (!string.IsNullOrEmpty(Model.CancellationReason))
        {
            <div><strong>Storno-Grund:</strong> @Model.CancellationReason</div>
        }
    </div>
</div>

<form method="post" asp-action="Close" asp-route-id="@Model.Id">
    @Html.AntiForgeryToken()
    <input type="hidden" name="rowVersion" value="@Convert.ToBase64String(Model.RowVersion)" />
    <table class="table table-sm">
        <thead>
            <tr><th>Pos</th><th>Artikel-Nr</th><th>Bezeichnung</th><th>Bestellt</th><th>Ist</th><th>ME</th><th>Lagerplatz</th></tr>
        </thead>
        <tbody>
            @foreach (var i in Model.Items)
            {
                <tr>
                    <td>@i.Position</td>
                    <td>@i.ArticleNumber</td>
                    <td>@i.ArticleDescription</td>
                    <td>@i.QuantityRequested</td>
                    <td>
                        <input type="hidden" name="itemIds" value="@i.Id" />
                        @if (isSubmitted)
                        {
                            <input type="number" name="quantitiesPicked" step="0.01" value="@(i.QuantityPicked ?? i.QuantityRequested)" class="form-control form-control-sm" style="width:90px;" />
                        }
                        else
                        {
                            @(i.QuantityPicked?.ToString() ?? "—")
                        }
                    </td>
                    <td>@i.Unit</td>
                    <td><small>@i.StorageLocations</small></td>
                </tr>
            }
        </tbody>
    </table>

    <a asp-action="Print" asp-route-id="@Model.Id" target="_blank" class="btn btn-secondary">Drucken</a>

    @if (isSubmitted)
    {
        <button type="submit" class="btn btn-success">Abschliessen</button>
        <button type="button" class="btn btn-outline-danger" data-bs-toggle="modal" data-bs-target="#cancel-modal">Stornieren</button>
    }
</form>

<div class="modal fade" id="cancel-modal" tabindex="-1">
    <div class="modal-dialog">
        <form method="post" asp-action="Cancel" asp-route-id="@Model.Id">
            @Html.AntiForgeryToken()
            <input type="hidden" name="rowVersion" value="@Convert.ToBase64String(Model.RowVersion)" />
            <div class="modal-content">
                <div class="modal-header"><h5 class="modal-title">Liste stornieren</h5></div>
                <div class="modal-body">
                    <label class="form-label">Grund (optional)</label>
                    <textarea name="reason" class="form-control" rows="3"></textarea>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Abbrechen</button>
                    <button type="submit" class="btn btn-danger">Stornieren</button>
                </div>
            </div>
        </form>
    </div>
</div>
```

- [ ] **Step 3: Print.cshtml** (analog `PrintBom.cshtml`)

```html
@model IdealAkeWms.Models.ViewModels.WarehouseRequisitionDetailViewModel
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
    </div>
    <table>
        <thead>
            <tr><th>Pos</th><th>Artikel-Nr</th><th>Bezeichnung</th><th>Bestellt</th><th>Ist</th><th>ME</th><th>Lagerplatz</th><th>Notiz</th></tr>
        </thead>
        <tbody>
        @foreach (var i in Model.Items)
        {
            <tr>
                <td>@i.Position</td>
                <td>@i.ArticleNumber</td>
                <td>@i.ArticleDescription</td>
                <td>@i.QuantityRequested</td>
                <td>@(i.QuantityPicked?.ToString() ?? "")</td>
                <td>@i.Unit</td>
                <td>@i.StorageLocations</td>
                <td></td>
            </tr>
        }
        </tbody>
    </table>
    <script>window.addEventListener('load', () => { setTimeout(() => window.print(), 300); });</script>
</body>
</html>
```

- [ ] **Step 4: Build + Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

- [ ] **Step 5: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Views/WarehousePicking/
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(warehouse): Lager-Views (Index/Details/Print)"
```

---

## Task 11: Layout-Menü + `WarehouseRequisitionEmailService` (Worker)

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`
- Create: `IDEALAKEWMSService/Services/IWarehouseRequisitionEmailService.cs`
- Create: `IDEALAKEWMSService/Services/WarehouseRequisitionEmailService.cs`
- Modify: `IDEALAKEWMSService/Workers/SyncWorker.cs`
- Modify: `IDEALAKEWMSService/Program.cs` (DI)

- [ ] **Step 1: Menü-Umbau in `_Layout.cshtml`**

Den existierenden flachen `<li>`-Eintrag für PartRequisitions (gefunden via `grep -n "asp-controller=\"PartRequisitions\"" _Layout.cshtml`) ersetzen durch ein Dropdown:

```html
@if (bestellungenAktiv && (canPick || canAccessStock))
{
    <li class="nav-item dropdown">
        <a class="nav-link dropdown-toggle @((ViewContext.RouteData.Values["controller"]?.ToString() is "PartRequisitions" or "WarehouseRequisitions" or "WarehousePicking") ? "active" : "")" href="#" role="button" data-bs-toggle="dropdown" aria-expanded="false">
            Bestellungen
        </a>
        <ul class="dropdown-menu">
            <li><a class="dropdown-item" asp-controller="PartRequisitions" asp-action="Index">Bedarfsmeldungen</a></li>
            <li><a class="dropdown-item" asp-controller="WarehouseRequisitions" asp-action="Index">Lagerbestellungen</a></li>
            @if (canAccessStock)
            {
                <li><hr class="dropdown-divider" /></li>
                <li><a class="dropdown-item" asp-controller="WarehousePicking" asp-action="Index">Lager: Eingehende Listen</a></li>
            }
        </ul>
    </li>
}
```

- [ ] **Step 2: Email-Service-Interface**

`IDEALAKEWMSService/Services/IWarehouseRequisitionEmailService.cs`:

```csharp
namespace IDEALAKEWMSService.Services;

public interface IWarehouseRequisitionEmailService
{
    Task<EmailResult> SendPendingEmailsAsync(bool dryRun, CancellationToken ct = default);
}

public record EmailResult(int SubmitsSent, int CancellationsSent, List<string> Errors);
```

- [ ] **Step 3: Email-Service-Implementation**

`IDEALAKEWMSService/Services/WarehouseRequisitionEmailService.cs`:

```csharp
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace IDEALAKEWMSService.Services;

public class WarehouseRequisitionEmailService : IWarehouseRequisitionEmailService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IMailService _mail;
    private readonly IConfiguration _config;
    private readonly ILogger<WarehouseRequisitionEmailService> _logger;

    public WarehouseRequisitionEmailService(
        ApplicationDbContext ctx,
        IWarehouseRequisitionRepository repo,
        IMailService mail,
        IConfiguration config,
        ILogger<WarehouseRequisitionEmailService> logger)
    {
        _ctx = ctx; _repo = repo; _mail = mail; _config = config; _logger = logger;
    }

    public async Task<EmailResult> SendPendingEmailsAsync(bool dryRun, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var baseUrl = await GetBaseUrlAsync(ct);

        // Submit-Mails
        var submits = await _repo.GetPendingSubmitEmailsAsync();
        var submitCount = 0;
        foreach (var r in submits)
        {
            try
            {
                var emails = r.OrderRecipientGroup!.Recipients.Where(x => x.IsActive).Select(x => x.Email).Distinct().ToList();
                if (emails.Count == 0)
                {
                    errors.Add($"Lagerbestellung #{r.Id}: keine aktiven Empfaenger.");
                    continue;
                }
                var subject = $"Lagerbestellung #{r.Id} — Werkbank {r.ProductionWorkplace.Name}";
                var body = BuildSubmitBody(r, baseUrl);
                if (!dryRun)
                {
                    await _mail.SendAsync(subject, body, emails, ct);
                    await _repo.MarkEmailSentAsync(r.Id, DateTime.Now);
                }
                submitCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Submit-Mail fuer Lagerbestellung {Id} fehlgeschlagen.", r.Id);
                errors.Add($"#{r.Id}: {ex.Message}");
            }
        }

        // Storno-Mails (nur wenn vorher Submit-Mail rausging)
        var cancels = await _repo.GetPendingCancellationEmailsAsync();
        var cancelCount = 0;
        foreach (var r in cancels)
        {
            try
            {
                var emails = r.OrderRecipientGroup!.Recipients.Where(x => x.IsActive).Select(x => x.Email).Distinct().ToList();
                if (emails.Count == 0)
                {
                    errors.Add($"Storno #{r.Id}: keine aktiven Empfaenger.");
                    continue;
                }
                var subject = $"[STORNO] Lagerbestellung #{r.Id} — Werkbank {r.ProductionWorkplace.Name}";
                var body = BuildCancellationBody(r, baseUrl);
                if (!dryRun)
                {
                    await _mail.SendAsync(subject, body, emails, ct);
                    await _repo.MarkCancellationEmailSentAsync(r.Id, DateTime.Now);
                }
                cancelCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Storno-Mail fuer Lagerbestellung {Id} fehlgeschlagen.", r.Id);
                errors.Add($"#{r.Id}: {ex.Message}");
            }
        }

        return new EmailResult(submitCount, cancelCount, errors);
    }

    private async Task<string> GetBaseUrlAsync(CancellationToken ct)
    {
        var connectionString = _config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString)) return "";
        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
            "SELECT [Value] FROM [ServiceSettings] WHERE [Key] = @Key", conn);
        cmd.Parameters.AddWithValue("@Key", "Notifications:AppBaseUrl");
        var v = await cmd.ExecuteScalarAsync(ct);
        return v?.ToString() ?? "";
    }

    private static string BuildSubmitBody(WarehouseRequisition r, string baseUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<html><body style='font-family:Segoe UI, Arial; color:#000;'>");
        sb.AppendLine($"<h2 style='color:#053153;'>Lagerbestellung #{r.Id}</h2>");
        sb.AppendLine($"<p><strong>Werkbank:</strong> {r.ProductionWorkplace.Name}<br />");
        sb.AppendLine($"<strong>Erfasser:</strong> {r.CreatedBy}<br />");
        sb.AppendLine($"<strong>Submit:</strong> {r.SubmittedAt:dd.MM.yyyy HH:mm}</p>");
        sb.AppendLine("<table style='border-collapse:collapse; border:1px solid #888;'>");
        sb.AppendLine("<thead><tr style='background:#f0f0f0;'><th style='border:1px solid #888; padding:4px;'>Pos</th><th style='border:1px solid #888; padding:4px;'>Artikel-Nr</th><th style='border:1px solid #888; padding:4px;'>Bezeichnung</th><th style='border:1px solid #888; padding:4px;'>Menge</th><th style='border:1px solid #888; padding:4px;'>ME</th></tr></thead><tbody>");
        foreach (var i in r.Items.OrderBy(i => i.Position))
        {
            sb.AppendLine($"<tr><td style='border:1px solid #888; padding:4px;'>{i.Position}</td><td style='border:1px solid #888; padding:4px;'>{i.ArticleNumber}</td><td style='border:1px solid #888; padding:4px;'>{i.ArticleDescription}</td><td style='border:1px solid #888; padding:4px;'>{i.QuantityRequested}</td><td style='border:1px solid #888; padding:4px;'>{i.Unit}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");
        if (!string.IsNullOrEmpty(baseUrl))
        {
            sb.AppendLine($"<p style='margin-top:20px;'><a href='{baseUrl}/WarehousePicking/Details/{r.Id}' style='display:inline-block;background:#43A6E2;color:#fff;padding:10px 20px;border-radius:4px;text-decoration:none;'>Lagerbestellung oeffnen</a></p>");
        }
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string BuildCancellationBody(WarehouseRequisition r, string baseUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<html><body style='font-family:Segoe UI, Arial; color:#000;'>");
        sb.AppendLine($"<h2 style='color:#c0392b;'>[STORNO] Lagerbestellung #{r.Id}</h2>");
        sb.AppendLine($"<p><strong>Werkbank:</strong> {r.ProductionWorkplace.Name}<br />");
        sb.AppendLine($"<strong>Erfasser:</strong> {r.CreatedBy}<br />");
        sb.AppendLine($"<strong>Storniert:</strong> {r.CancelledAt:dd.MM.yyyy HH:mm}</p>");
        if (!string.IsNullOrEmpty(r.CancellationReason))
        {
            sb.AppendLine($"<p><strong>Grund:</strong> {r.CancellationReason}</p>");
        }
        sb.AppendLine("<p><strong>Bitte nicht weiter bearbeiten.</strong></p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
```

- [ ] **Step 4: SyncWorker-Tick erweitern**

In `IDEALAKEWMSService/Workers/SyncWorker.cs` direkt NACH dem `Sync:PartRequisitionEmailEnabled`-Block (Zeilen ~110-123):

```csharp
// --- Lagerbestellungen E-Mail-Versand ---
if (_configuration.GetValue<bool>("Sync:WarehouseRequisitionEmailEnabled", false))
{
    try
    {
        var emailService = scope.ServiceProvider.GetRequiredService<IWarehouseRequisitionEmailService>();
        var result = await emailService.SendPendingEmailsAsync(dryRun, stoppingToken);
        if (result.SubmitsSent > 0 || result.CancellationsSent > 0)
            _logger.LogInformation("Lagerbestellungen: {Submits} Submit + {Cancels} Storno-Mails versendet.", result.SubmitsSent, result.CancellationsSent);
        foreach (var e in result.Errors) _logger.LogWarning("Lagerbestellung-Mail-Fehler: {Err}", e);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Fehler beim Versand der Lagerbestellungs-E-Mails.");
    }
}
```

- [ ] **Step 5: DI in Service-Program.cs**

In `IDEALAKEWMSService/Program.cs` bei den anderen Service-Registrierungen:

```csharp
builder.Services.AddScoped<IWarehouseRequisitionEmailService, WarehouseRequisitionEmailService>();
```

`IWarehouseRequisitionRepository` muss ebenfalls im Service-Projekt registriert werden, falls nicht bereits via Web-Project-Reference automatisch da:

```csharp
builder.Services.AddScoped<IWarehouseRequisitionRepository, WarehouseRequisitionRepository>();
```

- [ ] **Step 6: Build + Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -5
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

- [ ] **Step 7: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/Views/Shared/_Layout.cshtml IDEALAKEWMSService/Services/IWarehouseRequisitionEmailService.cs IDEALAKEWMSService/Services/WarehouseRequisitionEmailService.cs IDEALAKEWMSService/Workers/SyncWorker.cs IDEALAKEWMSService/Program.cs
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "feat(warehouse): WarehouseRequisitionEmailService + Layout-Dropdown 'Bestellungen'"
```

---

## Task 12: AppVersion + Docs + TESTSZENARIEN

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `PROJECT_STATUS.md`
- Modify: `CLAUDE.md`
- Modify: `docs/TESTSZENARIEN.md`

- [ ] **Step 1: AppVersion 1.8.4**

Beide Files: `Version = "1.8.4"`, `Date = "2026-04-30"`.

- [ ] **Step 2: CLAUDE.md AppSettings + Service-Konfiguration**

Im AppSettings-Block:

```markdown
| `DefaultLagerbestellempfaengerId` | (leer) | OrderRecipientGroup-ID fuer Lagerbestellungen (leer = Submit blockt) |
```

Im Service-Konfiguration-Block:

```markdown
| `Sync:WarehouseRequisitionEmailEnabled` | `false` | Aktiviert E-Mail-Versand fuer Lagerbestellungen im SyncWorker |
```

Zugriffsschutz-Tabelle (sofern existent) erweitern:

```markdown
| `[RequirePickingOrStockAccess]` | picking ODER stock | … WarehouseRequisitionsController |
| `[RequireStockAccess]` | admin, stock, stock_keyuser, picking | … WarehousePickingController |
```

- [ ] **Step 3: Help/Index.cshtml — neue Section "Lagerbestellungen"**

```html
<h5 class="mt-4">Lagerbestellungen</h5>
<p>Produktionsmitarbeiter erfassen Lagerartikel als Bestellliste fuer ihren Werkbankplatz, das Lager kommissioniert und schliesst die Liste mit Pro-Position-Ist-Mengen ab.</p>

<h6 class="mt-3">Erfassen</h6>
<ol>
    <li>Hauptmenue &rarr; Bestellungen &rarr; Lagerbestellungen.</li>
    <li>Werkbank waehlen (bei mehreren Zuordnungen) und "+ Neue Liste" klicken.</li>
    <li>Artikel ueber das Suchfeld auswaehlen, Menge anpassen.</li>
    <li>"Abschicken" sendet die Liste an den Default-Lagerbestellempfaenger (max. 15 Min Verzoegerung).</li>
</ol>

<h6 class="mt-3">Lager-Workflow</h6>
<p>Hauptmenue &rarr; Bestellungen &rarr; Lager: Eingehende Listen. Detail oeffnen, kommissionieren, "Drucken" fuer den Pickup-Druck, dann mit Pro-Position-Ist-Mengen "Abschliessen". Storno mit optionalem Grund moeglich.</p>

<h6 class="mt-3">Storno</h6>
<p>Erfasser oder Lager koennen eine Liste stornieren. Wenn die Liste vorher abgeschickt war, wird eine [STORNO]-E-Mail an den Empfaenger versendet.</p>

<h6 class="mt-3">Konfiguration</h6>
<ul>
    <li><code>DefaultLagerbestellempfaengerId</code> (Einstellungen) — ID der Empfaenger-Gruppe.</li>
    <li><code>Sync:WarehouseRequisitionEmailEnabled</code> (Service) — aktiviert den Mail-Versand.</li>
</ul>
```

- [ ] **Step 4: Changelog v1.8.4**

```html
<h5>v1.8.4 &mdash; 30.04.2026</h5>
<ul>
    <li><strong>Lagerbestellung aus der Produktion:</strong> neuer Workflow Bestellungen &rarr; Lagerbestellungen (Erfasser-Maske mit Werkbank-Auto-Resolution + Artikel-Suche) und Bestellungen &rarr; Lager: Eingehende Listen (Lager-Sicht mit Print + Pro-Position-Ist-Mengen + Storno).</li>
    <li>E-Mail-Versand bei Submit (Deep-Link zur Detail-Seite) und Storno ([STORNO]-Prefix).</li>
    <li>Default-Empfaenger via AppSetting <code>DefaultLagerbestellempfaengerId</code>.</li>
    <li>Top-Level-Menue "Bestellungen" wurde Dropdown mit drei Untermenues (Bedarfsmeldungen / Lagerbestellungen / Lager: Eingehende Listen).</li>
</ul>
```

- [ ] **Step 5: PROJECT_STATUS.md neuer Block (30.04.2026 v1.8.4)**

Im PROJECT_STATUS einen neuen Bullet-Block für v1.8.4 ergänzen mit den Hauptfeatures.

- [ ] **Step 6: TESTSZENARIEN.md neuer Bereich**

Suche höchste Bereich-Nummer (`grep -E "^## [0-9]+\." docs/TESTSZENARIEN.md | tail -5`), nutze N+1.

```markdown
## NN. Lagerbestellung aus der Produktion

### TS-NN.1 — Erfassen + Submit (1-Werkbank-User)
Vorbedingungen: User mit genau 1 Werkbank-Zuordnung. AppSetting DefaultLagerbestellempfaengerId gesetzt + Empfaengergruppe mit aktivem Recipient.
Schritte: Bestellungen &rarr; Lagerbestellungen &rarr; "+ Neue Liste". 2 Artikel ueber Suche hinzufuegen. "Abschicken".
Erwartet: Liste in Status "Abgeschickt" sichtbar im Erfasser-Index. Lager-Index zeigt sie unter "Submitted".

### TS-NN.2 — Werkbank-Auswahl bei N≥2
Vorbedingungen: User mit 2+ Werkbank-Zuordnungen.
Schritte: "+ Neue Liste" ohne Werkbank waehlen.
Erwartet: WarningMessage "Bitte Werkbank waehlen". Nach Auswahl: Liste wird angelegt.

### TS-NN.3 — 0 Werkbank-Zuordnungen
Vorbedingungen: User ohne Werkbank-Zuordnung.
Schritte: "+ Neue Liste".
Erwartet: WarningMessage "Bitte Werkbank-Zuordnung in Stammdaten pflegen".

### TS-NN.4 — Duplikat-Artikel
Schritte: zwei Mal denselben Artikel hinzufuegen.
Erwartet: Beim zweiten Klick Toast/Alert "Artikel ist bereits in der Liste".

### TS-NN.5 — Submit-Mail
Vorbedingungen: TS-NN.1, Service-Setting Sync:WarehouseRequisitionEmailEnabled=true.
Schritte: max. 15 Min warten oder Worker-Tick triggern.
Erwartet: E-Mail im Postfach des Empfaengers, Subject "Lagerbestellung #X — Werkbank Y", Body mit Items + Deep-Link.

### TS-NN.6 — Storno-Mail
Schritte: nach Submit (TS-NN.1) im Erfasser-Edit "Stornieren" mit Grund.
Erwartet: Liste Status "Storniert". Nach Worker-Tick zweite Mail mit Subject "[STORNO] …".

### TS-NN.7 — Lager: Detail + Print + Close
Schritte: Lager-Detail oeffnen, Pro-Position Ist-Menge anpassen, Drucken, Abschliessen.
Erwartet: Print-Tab oeffnet mit A4-Layout, Submit setzt Status "Erledigt", Items.QuantityPicked geschrieben.

### TS-NN.8 — RowVersion-Konflikt
Schritte: Detail in zwei Tabs oeffnen. Tab 1 Schliessen, Tab 2 Stornieren.
Erwartet: Tab 2 zeigt WarningMessage "Bestellung wurde inzwischen geaendert — bitte Liste neu laden."

### TS-NN.9 — AppSetting nicht gesetzt
Vorbedingungen: DefaultLagerbestellempfaengerId leer.
Schritte: Submit.
Erwartet: WarningMessage "Default-Lagerbestellempfaenger nicht konfiguriert".
```

- [ ] **Step 7: Build + Tests**

```bash
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet build --nologo 2>&1 | tail -3
cd "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" && dotnet test --nologo --no-build 2>&1 | tail -5
```

- [ ] **Step 8: Commit**

```bash
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs CLAUDE.md IdealAkeWms/Views/Help/ PROJECT_STATUS.md docs/TESTSZENARIEN.md
git -C "C:/Git/IDEAL-AKE-WMS_WT_bde-phase-1" commit -m "chore(warehouse): v1.8.4 docs + AppVersion + TESTSZENARIEN"
```

---

## Final Summary

12 Tasks, ~22 neue Unit-Tests + 9 manuelle Szenarien. Versions-Bump 1.8.3 → 1.8.4.

### Self-Review

1. **Spec coverage:**
   - §3 Datenmodell → Task 1
   - §4 Lifecycle + Transitions → Task 4 (Repo) + Tasks 6/7 (Controller-Validierung)
   - §5 Komponenten → Tasks 1, 4, 5, 6, 7, 8, 9, 10, 11
   - §6 AppSettings → Task 2
   - §7 Datenfluss Erfasser → Task 6 + 9
   - §7 Datenfluss Lager → Task 7 + 10
   - §7 Datenfluss Worker → Task 11
   - §8 Repository-Methoden → Task 4
   - §9 UI-Layout → Tasks 9 + 10
   - §10 Tests → Tasks 3, 4, 6, 7 (Unit) + Task 12 (Manual)
   - §11 Versionierung & Docs → Task 12
   - §12 Out of Scope → keine Tasks
   - §13 Risiken → in Tasks 6/7 (Validierung) + Task 11 (Worker-Errors)
   - §14 Erfolgskriterien → manuell verifizierbar via TESTSZENARIEN

2. **Placeholder scan:** keine TBDs/TODOs in Tasks. Alle Code-Blöcke vollständig.

3. **Type consistency:**
   - `WarehouseRequisitionStatus` (Enum), `WarehouseRequisition`, `WarehouseRequisitionItem` konsistent zwischen Models / Repository / Controllers / Views / Service
   - Repository-Methodensignatur identisch zwischen Interface (Task 4 Step 3) und Impl (Task 4 Step 4)
   - VM-Property `RowVersion` ist `byte[]` durchgängig
   - Email-Service: `EmailResult(SubmitsSent, CancellationsSent, Errors)` konsistent zwischen Interface und Verwendung in SyncWorker

4. **Bekannte Adaption-Stelle:** Task 4 Step 4 enthält den Hinweis "Lese die exakte Property von AuditableEntity" — der Implementer muss `cat AuditableEntity.cs` und ggf. die `GetForUserAsync`-Logik geringfügig anpassen, falls dort tatsächlich `int? CreatedByUserId` existiert oder nicht. Der Default ist String-basiert; Service-Layer übergibt den DisplayName.
