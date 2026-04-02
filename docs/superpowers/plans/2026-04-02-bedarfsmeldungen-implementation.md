# Bedarfsmeldungen aus Stückliste — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fehlteile aus der BOM per Button als interne Bedarfsmeldung erfassen, per E-Mail an konfigurierbare Empfängergruppen versenden und beim Wareneingang mit der Einbuchung verknüpfen.

**Architecture:** 4 neue DB-Tabellen (OrderRecipientGroups, OrderRecipients, ArticleGroupRecipientMappings, PartRequisitions), Repository Pattern, MVC-Controller für Stammdaten + Bestellübersicht, API-Controller für AJAX-Interaktionen aus der BOM-View, E-Mail-Versand über bestehenden MailService im IDEALAKEWMSService.

**Tech Stack:** ASP.NET Core 10.0, EF Core 10.0, SQL Server, Bootstrap 5, MailKit (bereits vorhanden), xUnit + Moq + FluentAssertions

**Spec:** `docs/superpowers/specs/2026-04-02-bedarfsmeldungen-design.md`

---

## File Structure

### New Files (Models)
- `IdealAkeWms/Models/OrderRecipientGroup.cs` — Entity (AuditableEntity)
- `IdealAkeWms/Models/OrderRecipient.cs` — Entity (AuditableEntity)
- `IdealAkeWms/Models/ArticleGroupRecipientMapping.cs` — Entity (AuditableEntity)
- `IdealAkeWms/Models/PartRequisition.cs` — Entity (AuditableEntity)
- `IdealAkeWms/Models/PartRequisitionPriority.cs` — Statische Konstanten
- `IdealAkeWms/Models/PartRequisitionStatus.cs` — Statische Konstanten

### New Files (ViewModels)
- `IdealAkeWms/Models/ViewModels/OrderRecipientViewModels.cs` — ViewModels für Empfänger-CRUD
- `IdealAkeWms/Models/ViewModels/PartRequisitionViewModels.cs` — ViewModels für Bestellübersicht + Modal

### New Files (Repositories)
- `IdealAkeWms/Data/Repositories/IOrderRecipientRepository.cs` — Interface
- `IdealAkeWms/Data/Repositories/OrderRecipientRepository.cs` — Implementation
- `IdealAkeWms/Data/Repositories/IPartRequisitionRepository.cs` — Interface
- `IdealAkeWms/Data/Repositories/PartRequisitionRepository.cs` — Implementation

### New Files (Controllers)
- `IdealAkeWms/Controllers/OrderRecipientsController.cs` — Stammdaten-CRUD (MVC)
- `IdealAkeWms/Controllers/PartRequisitionsController.cs` — Bestellübersicht (MVC)
- `IdealAkeWms/Controllers/Api/PartRequisitionsApiController.cs` — AJAX für BOM-Modal

### New Files (Views)
- `IdealAkeWms/Views/OrderRecipients/Index.cshtml` — Empfängergruppen-Liste
- `IdealAkeWms/Views/OrderRecipients/Create.cshtml` — Gruppe erstellen
- `IdealAkeWms/Views/OrderRecipients/Edit.cshtml` — Gruppe bearbeiten + Empfänger
- `IdealAkeWms/Views/OrderRecipients/ArticleGroupMappings.cshtml` — Zuordnungen
- `IdealAkeWms/Views/PartRequisitions/Index.cshtml` — Bestellübersicht

### New Files (Filters)
- `IdealAkeWms/Filters/RequirePickingOrStockAccessAttribute.cs` — Für Bestellübersicht

### New Files (Service)
- `IDEALAKEWMSService/Services/IPartRequisitionEmailService.cs` — Interface
- `IDEALAKEWMSService/Services/PartRequisitionEmailService.cs` — E-Mail-Versand

### New Files (SQL)
- `SQL/36_AddPartRequisitions.sql` — Migration für alle 4 Tabellen + Indizes

### New Files (Tests)
- `IdealAkeWms.Tests/Repositories/PartRequisitionRepositoryTests.cs`
- `IdealAkeWms.Tests/Repositories/OrderRecipientRepositoryTests.cs`

### Modified Files
- `IdealAkeWms/Data/ApplicationDbContext.cs:32` — 4 neue DbSets + OnModelCreating
- `IdealAkeWms/Program.cs:54` — DI-Registrierung
- `IdealAkeWms/Views/Shared/_Layout.cshtml:82` — Menüpunkte
- `IdealAkeWms/Views/ProductionOrders/Bom.cshtml:80` — Bestell-Buttons + Modal + Badge
- `IdealAkeWms/Views/StockMovements/Inbound.cshtml` — Offene Bedarfsmeldungen
- `IdealAkeWms/Controllers/ProductionOrdersController.cs:180` — Bedarfsmeldungen in BOM laden
- `IdealAkeWms/Controllers/StockMovementsController.cs:73` — Offene Meldungen laden
- `IdealAkeWms/AppVersion.cs` — Version hochzählen
- `IDEALAKEWMSService/AppVersion.cs` — Version hochzählen
- `IDEALAKEWMSService/Workers/SyncWorker.cs:79` — E-Mail-Versand aufrufen
- `IDEALAKEWMSService/Program.cs` — DI für PartRequisitionEmailService
- `IdealAkeWms/Views/Help/Changelog.cshtml` — Changelog ergänzen
- `IdealAkeWms/Views/Help/Index.cshtml` — Hilfeseite ergänzen
- `SQL/00_FreshInstall.sql` — Konsolidieren
- `CLAUDE.md` — Dokumentation ergänzen
- `PROJECT_STATUS.md` — Status aktualisieren

---

## Task 1: Models + Statische Konstanten

**Files:**
- Create: `IdealAkeWms/Models/PartRequisitionStatus.cs`
- Create: `IdealAkeWms/Models/PartRequisitionPriority.cs`
- Create: `IdealAkeWms/Models/OrderRecipientGroup.cs`
- Create: `IdealAkeWms/Models/OrderRecipient.cs`
- Create: `IdealAkeWms/Models/ArticleGroupRecipientMapping.cs`
- Create: `IdealAkeWms/Models/PartRequisition.cs`

- [ ] **Step 1: Statische Konstanten erstellen**

`IdealAkeWms/Models/PartRequisitionStatus.cs`:
```csharp
namespace IdealAkeWms.Models;

public static class PartRequisitionStatus
{
    public const string Offen = "Offen";
    public const string Erfuellt = "Erfuellt";
    public const string Storniert = "Storniert";
}
```

`IdealAkeWms/Models/PartRequisitionPriority.cs`:
```csharp
namespace IdealAkeWms.Models;

public static class PartRequisitionPriority
{
    public const string Normal = "Normal";
    public const string Dringend = "Dringend";
    public const string Eilt = "Eilt";
}
```

- [ ] **Step 2: Entity-Models erstellen**

`IdealAkeWms/Models/OrderRecipientGroup.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class OrderRecipientGroup : AuditableEntity
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public ICollection<OrderRecipient> Recipients { get; set; } = new List<OrderRecipient>();
    public ICollection<ArticleGroupRecipientMapping> ArticleGroupMappings { get; set; } = new List<ArticleGroupRecipientMapping>();
}
```

`IdealAkeWms/Models/OrderRecipient.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class OrderRecipient : AuditableEntity
{
    public int OrderRecipientGroupId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public OrderRecipientGroup OrderRecipientGroup { get; set; } = null!;
}
```

`IdealAkeWms/Models/ArticleGroupRecipientMapping.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ArticleGroupRecipientMapping : AuditableEntity
{
    [Required]
    [StringLength(100)]
    public string ArticleGroup { get; set; } = string.Empty;

    public int OrderRecipientGroupId { get; set; }
    public OrderRecipientGroup OrderRecipientGroup { get; set; } = null!;
}
```

`IdealAkeWms/Models/PartRequisition.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class PartRequisition : AuditableEntity
{
    public int ProductionOrderId { get; set; }

    [Required]
    [StringLength(100)]
    public string ArticleNumber { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ArticleDescription { get; set; }

    [StringLength(100)]
    public string? ArticleGroup { get; set; }

    [StringLength(50)]
    public string? Position { get; set; }

    public decimal Quantity { get; set; }

    [StringLength(20)]
    public string? Unit { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = PartRequisitionStatus.Offen;

    [Required]
    [StringLength(20)]
    public string Priority { get; set; } = PartRequisitionPriority.Normal;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public int? OrderRecipientGroupId { get; set; }

    [StringLength(1000)]
    public string? SentToEmails { get; set; }

    public DateTime? EmailSentAt { get; set; }

    public int? FulfilledByStockMovementId { get; set; }
    public DateTime? FulfilledAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    [StringLength(200)]
    public string? CancelledBy { get; set; }

    // Navigation
    public ProductionOrder ProductionOrder { get; set; } = null!;
    public OrderRecipientGroup? OrderRecipientGroup { get; set; }
    public StockMovement? FulfilledByStockMovement { get; set; }
}
```

- [ ] **Step 3: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Erfolgreich (Models haben noch keine DbSet-Referenz, aber kompilieren eigenständig)

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Models/PartRequisitionStatus.cs IdealAkeWms/Models/PartRequisitionPriority.cs IdealAkeWms/Models/OrderRecipientGroup.cs IdealAkeWms/Models/OrderRecipient.cs IdealAkeWms/Models/ArticleGroupRecipientMapping.cs IdealAkeWms/Models/PartRequisition.cs
git commit -m "feat: add entity models for part requisitions and order recipients"
```

---

## Task 2: ApplicationDbContext + EF Migration

**Files:**
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs:32` — Neue DbSets
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs:34` — OnModelCreating erweitern

- [ ] **Step 1: DbSets hinzufügen**

Nach Zeile 32 in `ApplicationDbContext.cs` (nach `EnaioDmsDocuments`):

```csharp
public DbSet<OrderRecipientGroup> OrderRecipientGroups => Set<OrderRecipientGroup>();
public DbSet<OrderRecipient> OrderRecipients => Set<OrderRecipient>();
public DbSet<ArticleGroupRecipientMapping> ArticleGroupRecipientMappings => Set<ArticleGroupRecipientMapping>();
public DbSet<PartRequisition> PartRequisitions => Set<PartRequisition>();
```

- [ ] **Step 2: OnModelCreating-Konfiguration hinzufügen**

Am Ende von `OnModelCreating` (vor der schließenden Klammer) in `ApplicationDbContext.cs`:

```csharp
// OrderRecipientGroup
modelBuilder.Entity<OrderRecipientGroup>(entity =>
{
    entity.HasMany(g => g.Recipients)
        .WithOne(r => r.OrderRecipientGroup)
        .HasForeignKey(r => r.OrderRecipientGroupId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasMany(g => g.ArticleGroupMappings)
        .WithOne(m => m.OrderRecipientGroup)
        .HasForeignKey(m => m.OrderRecipientGroupId)
        .OnDelete(DeleteBehavior.Cascade);
});

// OrderRecipient
modelBuilder.Entity<OrderRecipient>(entity =>
{
    entity.HasIndex(e => e.OrderRecipientGroupId)
        .HasDatabaseName("IX_OrderRecipients_GroupId");
});

// ArticleGroupRecipientMapping
modelBuilder.Entity<ArticleGroupRecipientMapping>(entity =>
{
    entity.HasIndex(e => e.ArticleGroup)
        .HasDatabaseName("IX_ArticleGroupRecipientMappings_ArticleGroup");

    entity.HasIndex(e => new { e.ArticleGroup, e.OrderRecipientGroupId })
        .IsUnique()
        .HasDatabaseName("UX_ArticleGroupRecipientMappings_Group_Recipient");
});

// PartRequisition
modelBuilder.Entity<PartRequisition>(entity =>
{
    entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");

    entity.HasOne(e => e.ProductionOrder)
        .WithMany()
        .HasForeignKey(e => e.ProductionOrderId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(e => e.OrderRecipientGroup)
        .WithMany()
        .HasForeignKey(e => e.OrderRecipientGroupId)
        .OnDelete(DeleteBehavior.SetNull);

    entity.HasOne(e => e.FulfilledByStockMovement)
        .WithMany()
        .HasForeignKey(e => e.FulfilledByStockMovementId)
        .OnDelete(DeleteBehavior.SetNull);

    entity.HasIndex(e => e.ProductionOrderId)
        .HasDatabaseName("IX_PartRequisitions_ProductionOrderId");

    entity.HasIndex(e => e.ArticleNumber)
        .HasDatabaseName("IX_PartRequisitions_ArticleNumber");

    entity.HasIndex(e => e.Status)
        .HasDatabaseName("IX_PartRequisitions_Status");

    entity.HasIndex(e => new { e.EmailSentAt, e.Status })
        .HasDatabaseName("IX_PartRequisitions_EmailSentAt_Status");
});
```

- [ ] **Step 3: EF Migration erstellen**

Run: `cd IdealAkeWms && dotnet ef migrations add AddPartRequisitions`
Expected: Migration wird in `Migrations/` erstellt

- [ ] **Step 4: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Erfolgreich

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Data/ApplicationDbContext.cs IdealAkeWms/Migrations/
git commit -m "feat: add DbSets and EF migration for part requisitions"
```

---

## Task 3: SQL-Migrationsskript

**Files:**
- Create: `SQL/36_AddPartRequisitions.sql`

- [ ] **Step 1: SQL-Script erstellen**

`SQL/36_AddPartRequisitions.sql`:
```sql
-- =============================================
-- 36: Bedarfsmeldungen + Empfänger-Verwaltung
-- =============================================

-- OrderRecipientGroups
IF OBJECT_ID(N'[dbo].[OrderRecipientGroups]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderRecipientGroups] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [CreatedByWindows] nvarchar(200) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_OrderRecipientGroups] PRIMARY KEY ([Id])
    );
END
GO

-- OrderRecipients
IF OBJECT_ID(N'[dbo].[OrderRecipients]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderRecipients] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [OrderRecipientGroupId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Email] nvarchar(300) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [CreatedByWindows] nvarchar(200) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_OrderRecipients] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderRecipients_OrderRecipientGroups] FOREIGN KEY ([OrderRecipientGroupId])
            REFERENCES [dbo].[OrderRecipientGroups]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_OrderRecipients_GroupId] ON [dbo].[OrderRecipients]([OrderRecipientGroupId]);
END
GO

-- ArticleGroupRecipientMappings
IF OBJECT_ID(N'[dbo].[ArticleGroupRecipientMappings]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleGroupRecipientMappings] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ArticleGroup] nvarchar(100) NOT NULL,
        [OrderRecipientGroupId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [CreatedByWindows] nvarchar(200) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_ArticleGroupRecipientMappings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ArticleGroupRecipientMappings_OrderRecipientGroups] FOREIGN KEY ([OrderRecipientGroupId])
            REFERENCES [dbo].[OrderRecipientGroups]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_ArticleGroupRecipientMappings_ArticleGroup] ON [dbo].[ArticleGroupRecipientMappings]([ArticleGroup]);
    CREATE UNIQUE INDEX [UX_ArticleGroupRecipientMappings_Group_Recipient] ON [dbo].[ArticleGroupRecipientMappings]([ArticleGroup], [OrderRecipientGroupId]);
END
GO

-- PartRequisitions
IF OBJECT_ID(N'[dbo].[PartRequisitions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PartRequisitions] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ProductionOrderId] int NOT NULL,
        [ArticleNumber] nvarchar(100) NOT NULL,
        [ArticleDescription] nvarchar(500) NULL,
        [ArticleGroup] nvarchar(100) NULL,
        [Position] nvarchar(50) NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [Unit] nvarchar(20) NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT 'Offen',
        [Priority] nvarchar(20) NOT NULL DEFAULT 'Normal',
        [Notes] nvarchar(1000) NULL,
        [OrderRecipientGroupId] int NULL,
        [SentToEmails] nvarchar(1000) NULL,
        [EmailSentAt] datetime2 NULL,
        [FulfilledByStockMovementId] int NULL,
        [FulfilledAt] datetime2 NULL,
        [CancelledAt] datetime2 NULL,
        [CancelledBy] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [CreatedByWindows] nvarchar(200) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [ModifiedByWindows] nvarchar(200) NULL,
        CONSTRAINT [PK_PartRequisitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PartRequisitions_ProductionOrders] FOREIGN KEY ([ProductionOrderId])
            REFERENCES [dbo].[ProductionOrders]([Id]),
        CONSTRAINT [FK_PartRequisitions_OrderRecipientGroups] FOREIGN KEY ([OrderRecipientGroupId])
            REFERENCES [dbo].[OrderRecipientGroups]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_PartRequisitions_StockMovements] FOREIGN KEY ([FulfilledByStockMovementId])
            REFERENCES [dbo].[StockMovements]([Id]) ON DELETE SET NULL
    );

    CREATE INDEX [IX_PartRequisitions_ProductionOrderId] ON [dbo].[PartRequisitions]([ProductionOrderId]);
    CREATE INDEX [IX_PartRequisitions_ArticleNumber] ON [dbo].[PartRequisitions]([ArticleNumber]);
    CREATE INDEX [IX_PartRequisitions_Status] ON [dbo].[PartRequisitions]([Status]);
    CREATE INDEX [IX_PartRequisitions_EmailSentAt_Status] ON [dbo].[PartRequisitions]([EmailSentAt], [Status]);
END
GO

-- AppSetting: BestellungenAktiv
IF NOT EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'BestellungenAktiv')
BEGIN
    INSERT INTO [dbo].[AppSettings] ([Key], [Value], [Description])
    VALUES ('BestellungenAktiv', 'false', 'Bedarfsmeldungen aus Stückliste aktivieren');
END
GO

-- EF Migrations History
INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT '99999999999999_AddPartRequisitions', '10.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = '99999999999999_AddPartRequisitions'
);
GO
```

**Hinweis:** Die `MigrationId` muss nach `dotnet ef migrations add` mit dem tatsächlichen Wert aus dem `Migrations/`-Ordner ersetzt werden.

- [ ] **Step 2: Commit**

```bash
git add SQL/36_AddPartRequisitions.sql
git commit -m "feat: add SQL migration script for part requisitions tables"
```

---

## Task 4: Repository-Interfaces + Implementierungen

**Files:**
- Create: `IdealAkeWms/Data/Repositories/IOrderRecipientRepository.cs`
- Create: `IdealAkeWms/Data/Repositories/OrderRecipientRepository.cs`
- Create: `IdealAkeWms/Data/Repositories/IPartRequisitionRepository.cs`
- Create: `IdealAkeWms/Data/Repositories/PartRequisitionRepository.cs`

- [ ] **Step 1: IOrderRecipientRepository erstellen**

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IOrderRecipientRepository
{
    // Gruppen
    Task<List<OrderRecipientGroup>> GetAllGroupsAsync();
    Task<OrderRecipientGroup?> GetGroupByIdAsync(int id);
    Task AddGroupAsync(OrderRecipientGroup group);
    Task UpdateGroupAsync(OrderRecipientGroup group);
    Task<bool> DeleteGroupAsync(int id);
    Task<bool> GroupHasOpenRequisitionsAsync(int groupId);

    // Empfänger
    Task<OrderRecipient?> GetRecipientByIdAsync(int id);
    Task AddRecipientAsync(OrderRecipient recipient);
    Task UpdateRecipientAsync(OrderRecipient recipient);
    Task DeleteRecipientAsync(int id);

    // Mappings
    Task<List<ArticleGroupRecipientMapping>> GetMappingsAsync();
    Task<List<OrderRecipientGroup>> GetGroupsByArticleGroupAsync(string articleGroup);
    Task SetMappingsForArticleGroupAsync(string articleGroup, List<int> groupIds, string createdBy, string createdByWindows);

    // Alle bekannten Artikelgruppen
    Task<List<string>> GetDistinctArticleGroupsAsync();
}
```

- [ ] **Step 2: OrderRecipientRepository implementieren**

```csharp
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class OrderRecipientRepository : IOrderRecipientRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRecipientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderRecipientGroup>> GetAllGroupsAsync()
    {
        return await _context.OrderRecipientGroups
            .Include(g => g.Recipients)
            .Include(g => g.ArticleGroupMappings)
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    public async Task<OrderRecipientGroup?> GetGroupByIdAsync(int id)
    {
        return await _context.OrderRecipientGroups
            .Include(g => g.Recipients)
            .Include(g => g.ArticleGroupMappings)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task AddGroupAsync(OrderRecipientGroup group)
    {
        _context.OrderRecipientGroups.Add(group);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateGroupAsync(OrderRecipientGroup group)
    {
        _context.OrderRecipientGroups.Update(group);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteGroupAsync(int id)
    {
        var group = await _context.OrderRecipientGroups.FindAsync(id);
        if (group == null) return false;

        _context.OrderRecipientGroups.Remove(group);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> GroupHasOpenRequisitionsAsync(int groupId)
    {
        return await _context.PartRequisitions
            .AnyAsync(r => r.OrderRecipientGroupId == groupId && r.Status == PartRequisitionStatus.Offen);
    }

    public async Task<OrderRecipient?> GetRecipientByIdAsync(int id)
    {
        return await _context.OrderRecipients.FindAsync(id);
    }

    public async Task AddRecipientAsync(OrderRecipient recipient)
    {
        _context.OrderRecipients.Add(recipient);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRecipientAsync(OrderRecipient recipient)
    {
        _context.OrderRecipients.Update(recipient);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRecipientAsync(int id)
    {
        var recipient = await _context.OrderRecipients.FindAsync(id);
        if (recipient != null)
        {
            _context.OrderRecipients.Remove(recipient);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<ArticleGroupRecipientMapping>> GetMappingsAsync()
    {
        return await _context.ArticleGroupRecipientMappings
            .Include(m => m.OrderRecipientGroup)
            .OrderBy(m => m.ArticleGroup)
            .ToListAsync();
    }

    public async Task<List<OrderRecipientGroup>> GetGroupsByArticleGroupAsync(string articleGroup)
    {
        var groupIds = await _context.ArticleGroupRecipientMappings
            .Where(m => m.ArticleGroup == articleGroup)
            .Select(m => m.OrderRecipientGroupId)
            .ToListAsync();

        return await _context.OrderRecipientGroups
            .Include(g => g.Recipients.Where(r => r.IsActive))
            .Where(g => groupIds.Contains(g.Id))
            .ToListAsync();
    }

    public async Task SetMappingsForArticleGroupAsync(string articleGroup, List<int> groupIds, string createdBy, string createdByWindows)
    {
        var existing = await _context.ArticleGroupRecipientMappings
            .Where(m => m.ArticleGroup == articleGroup)
            .ToListAsync();

        _context.ArticleGroupRecipientMappings.RemoveRange(existing);

        foreach (var groupId in groupIds)
        {
            _context.ArticleGroupRecipientMappings.Add(new ArticleGroupRecipientMapping
            {
                ArticleGroup = articleGroup,
                OrderRecipientGroupId = groupId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                CreatedByWindows = createdByWindows
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<string>> GetDistinctArticleGroupsAsync()
    {
        return await _context.Articles
            .Where(a => !string.IsNullOrEmpty(a.ArticleGroup))
            .Select(a => a.ArticleGroup!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();
    }
}
```

- [ ] **Step 3: IPartRequisitionRepository erstellen**

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IPartRequisitionRepository
{
    Task<PartRequisition?> GetByIdAsync(int id);
    Task AddAsync(PartRequisition requisition);
    Task AddRangeAsync(IEnumerable<PartRequisition> requisitions);
    Task UpdateAsync(PartRequisition requisition);

    // Abfragen
    Task<List<PartRequisition>> GetByProductionOrderAsync(int productionOrderId);
    Task<List<PartRequisition>> GetOpenByArticleNumberAsync(string articleNumber);
    Task<bool> HasOpenRequisitionAsync(int productionOrderId, string articleNumber);

    // Bestellübersicht mit Pagination
    Task<(List<PartRequisition> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, bool showAll = false, string? searchTerm = null);

    // Für E-Mail-Versand (Service)
    Task<List<PartRequisition>> GetUnsentAsync();

    // Erfüllung
    Task FulfillAsync(int requisitionId, int stockMovementId, string modifiedBy, string modifiedByWindows);

    // Stornierung
    Task CancelAsync(int requisitionId, string cancelledBy, string modifiedBy, string modifiedByWindows);
}
```

- [ ] **Step 4: PartRequisitionRepository implementieren**

```csharp
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class PartRequisitionRepository : IPartRequisitionRepository
{
    private readonly ApplicationDbContext _context;

    public PartRequisitionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PartRequisition?> GetByIdAsync(int id)
    {
        return await _context.PartRequisitions
            .Include(r => r.ProductionOrder)
            .Include(r => r.OrderRecipientGroup)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task AddAsync(PartRequisition requisition)
    {
        _context.PartRequisitions.Add(requisition);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<PartRequisition> requisitions)
    {
        _context.PartRequisitions.AddRange(requisitions);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PartRequisition requisition)
    {
        _context.PartRequisitions.Update(requisition);
        await _context.SaveChangesAsync();
    }

    public async Task<List<PartRequisition>> GetByProductionOrderAsync(int productionOrderId)
    {
        return await _context.PartRequisitions
            .Where(r => r.ProductionOrderId == productionOrderId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PartRequisition>> GetOpenByArticleNumberAsync(string articleNumber)
    {
        return await _context.PartRequisitions
            .Include(r => r.ProductionOrder)
            .Where(r => r.ArticleNumber == articleNumber && r.Status == PartRequisitionStatus.Offen)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasOpenRequisitionAsync(int productionOrderId, string articleNumber)
    {
        return await _context.PartRequisitions
            .AnyAsync(r => r.ProductionOrderId == productionOrderId
                        && r.ArticleNumber == articleNumber
                        && r.Status == PartRequisitionStatus.Offen);
    }

    public async Task<(List<PartRequisition> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, bool showAll = false, string? searchTerm = null)
    {
        var query = _context.PartRequisitions
            .Include(r => r.ProductionOrder)
            .AsQueryable();

        if (!showAll)
            query = query.Where(r => r.Status == PartRequisitionStatus.Offen);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(r =>
                r.ArticleNumber.Contains(term) ||
                (r.ArticleDescription != null && r.ArticleDescription.Contains(term)) ||
                r.ProductionOrder.OrderNumber.Contains(term) ||
                (r.ProductionOrder.Customer != null && r.ProductionOrder.Customer.Contains(term)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<PartRequisition>> GetUnsentAsync()
    {
        return await _context.PartRequisitions
            .Include(r => r.ProductionOrder)
            .Where(r => r.EmailSentAt == null && r.Status == PartRequisitionStatus.Offen)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task FulfillAsync(int requisitionId, int stockMovementId, string modifiedBy, string modifiedByWindows)
    {
        var requisition = await _context.PartRequisitions.FindAsync(requisitionId);
        if (requisition == null || requisition.Status != PartRequisitionStatus.Offen) return;

        requisition.Status = PartRequisitionStatus.Erfuellt;
        requisition.FulfilledByStockMovementId = stockMovementId;
        requisition.FulfilledAt = DateTime.UtcNow;
        requisition.ModifiedAt = DateTime.UtcNow;
        requisition.ModifiedBy = modifiedBy;
        requisition.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public async Task CancelAsync(int requisitionId, string cancelledBy, string modifiedBy, string modifiedByWindows)
    {
        var requisition = await _context.PartRequisitions.FindAsync(requisitionId);
        if (requisition == null || requisition.Status != PartRequisitionStatus.Offen) return;

        requisition.Status = PartRequisitionStatus.Storniert;
        requisition.CancelledAt = DateTime.UtcNow;
        requisition.CancelledBy = cancelledBy;
        requisition.ModifiedAt = DateTime.UtcNow;
        requisition.ModifiedBy = modifiedBy;
        requisition.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }
}
```

- [ ] **Step 5: DI-Registrierung in Program.cs**

Nach Zeile 54 (nach `IEnaioDmsDocumentRepository`):

```csharp
builder.Services.AddScoped<IOrderRecipientRepository, OrderRecipientRepository>();
builder.Services.AddScoped<IPartRequisitionRepository, PartRequisitionRepository>();
```

- [ ] **Step 6: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Erfolgreich

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Data/Repositories/IOrderRecipientRepository.cs IdealAkeWms/Data/Repositories/OrderRecipientRepository.cs IdealAkeWms/Data/Repositories/IPartRequisitionRepository.cs IdealAkeWms/Data/Repositories/PartRequisitionRepository.cs IdealAkeWms/Program.cs
git commit -m "feat: add repositories for order recipients and part requisitions"
```

---

## Task 5: Repository-Tests

**Files:**
- Create: `IdealAkeWms.Tests/Repositories/PartRequisitionRepositoryTests.cs`
- Create: `IdealAkeWms.Tests/Repositories/OrderRecipientRepositoryTests.cs`

- [ ] **Step 1: PartRequisitionRepositoryTests erstellen**

```csharp
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Repositories;

public class PartRequisitionRepositoryTests
{
    private static async Task<(ApplicationDbContext context, PartRequisitionRepository repo, ProductionOrder order)> CreateRepoWithOrder()
    {
        var context = TestDbContextFactory.CreateContext();
        var order = new ProductionOrder
        {
            OrderNumber = "2607151",
            Quantity = 1,
            ArticleNumber = "S0310395",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        context.ProductionOrders.Add(order);
        await context.SaveChangesAsync();
        var repo = new PartRequisitionRepository(context);
        return (context, repo, order);
    }

    [Fact]
    public async Task AddAsync_CreatesRequisition()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        var requisition = new PartRequisition
        {
            ProductionOrderId = order.Id,
            ArticleNumber = "87050064",
            ArticleDescription = "Seitenwand",
            Quantity = 2,
            Status = PartRequisitionStatus.Offen,
            Priority = PartRequisitionPriority.Normal,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "testuser",
            CreatedByWindows = "DOMAIN\\testuser"
        };

        await repo.AddAsync(requisition);

        requisition.Id.Should().BeGreaterThan(0);
        var loaded = await repo.GetByIdAsync(requisition.Id);
        loaded.Should().NotBeNull();
        loaded!.ArticleNumber.Should().Be("87050064");
    }

    [Fact]
    public async Task GetOpenByArticleNumberAsync_ReturnsOnlyOpen()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        context.PartRequisitions.AddRange(
            new PartRequisition
            {
                ProductionOrderId = order.Id, ArticleNumber = "87050064", Quantity = 1,
                Status = PartRequisitionStatus.Offen, Priority = PartRequisitionPriority.Normal,
                CreatedAt = DateTime.UtcNow.AddHours(-2), CreatedBy = "a", CreatedByWindows = "a"
            },
            new PartRequisition
            {
                ProductionOrderId = order.Id, ArticleNumber = "87050064", Quantity = 1,
                Status = PartRequisitionStatus.Erfuellt, Priority = PartRequisitionPriority.Normal,
                CreatedAt = DateTime.UtcNow.AddHours(-1), CreatedBy = "a", CreatedByWindows = "a"
            },
            new PartRequisition
            {
                ProductionOrderId = order.Id, ArticleNumber = "99999999", Quantity = 1,
                Status = PartRequisitionStatus.Offen, Priority = PartRequisitionPriority.Normal,
                CreatedAt = DateTime.UtcNow, CreatedBy = "a", CreatedByWindows = "a"
            }
        );
        await context.SaveChangesAsync();

        var result = await repo.GetOpenByArticleNumberAsync("87050064");
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(PartRequisitionStatus.Offen);
    }

    [Fact]
    public async Task GetOpenByArticleNumberAsync_OrdersByCreatedAtAsc()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        context.PartRequisitions.AddRange(
            new PartRequisition
            {
                ProductionOrderId = order.Id, ArticleNumber = "87050064", Quantity = 3,
                Status = PartRequisitionStatus.Offen, Priority = PartRequisitionPriority.Normal,
                CreatedAt = DateTime.UtcNow, CreatedBy = "newer", CreatedByWindows = "a"
            },
            new PartRequisition
            {
                ProductionOrderId = order.Id, ArticleNumber = "87050064", Quantity = 1,
                Status = PartRequisitionStatus.Offen, Priority = PartRequisitionPriority.Normal,
                CreatedAt = DateTime.UtcNow.AddHours(-5), CreatedBy = "older", CreatedByWindows = "a"
            }
        );
        await context.SaveChangesAsync();

        var result = await repo.GetOpenByArticleNumberAsync("87050064");
        result.Should().HaveCount(2);
        result[0].CreatedBy.Should().Be("older");
        result[1].CreatedBy.Should().Be("newer");
    }

    [Fact]
    public async Task FulfillAsync_SetsStatusAndReference()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        var stockMovement = new StockMovement
        {
            ArticleId = 1, Quantity = 5, StorageLocationId = 1,
            MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
            WindowsUser = "test", CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t"
        };
        context.StockMovements.Add(stockMovement);
        await context.SaveChangesAsync();

        var requisition = new PartRequisition
        {
            ProductionOrderId = order.Id, ArticleNumber = "87050064", Quantity = 2,
            Status = PartRequisitionStatus.Offen, Priority = PartRequisitionPriority.Normal,
            CreatedAt = DateTime.UtcNow, CreatedBy = "a", CreatedByWindows = "a"
        };
        await repo.AddAsync(requisition);

        await repo.FulfillAsync(requisition.Id, stockMovement.Id, "fulfiller", "DOMAIN\\fulfiller");

        var loaded = await repo.GetByIdAsync(requisition.Id);
        loaded!.Status.Should().Be(PartRequisitionStatus.Erfuellt);
        loaded.FulfilledByStockMovementId.Should().Be(stockMovement.Id);
        loaded.FulfilledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelAsync_SetsStatusAndCancelledBy()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        var requisition = new PartRequisition
        {
            ProductionOrderId = order.Id, ArticleNumber = "87050064", Quantity = 2,
            Status = PartRequisitionStatus.Offen, Priority = PartRequisitionPriority.Normal,
            CreatedAt = DateTime.UtcNow, CreatedBy = "a", CreatedByWindows = "a"
        };
        await repo.AddAsync(requisition);

        await repo.CancelAsync(requisition.Id, "canceller", "canceller", "DOMAIN\\canceller");

        var loaded = await repo.GetByIdAsync(requisition.Id);
        loaded!.Status.Should().Be(PartRequisitionStatus.Storniert);
        loaded.CancelledBy.Should().Be("canceller");
        loaded.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task HasOpenRequisitionAsync_ReturnsTrueWhenExists()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        context.PartRequisitions.Add(new PartRequisition
        {
            ProductionOrderId = order.Id, ArticleNumber = "87050064", Quantity = 1,
            Status = PartRequisitionStatus.Offen, Priority = PartRequisitionPriority.Normal,
            CreatedAt = DateTime.UtcNow, CreatedBy = "a", CreatedByWindows = "a"
        });
        await context.SaveChangesAsync();

        (await repo.HasOpenRequisitionAsync(order.Id, "87050064")).Should().BeTrue();
        (await repo.HasOpenRequisitionAsync(order.Id, "99999999")).Should().BeFalse();
    }

    [Fact]
    public async Task GetUnsentAsync_ReturnsOnlyUnsentOpen()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        context.PartRequisitions.AddRange(
            new PartRequisition
            {
                ProductionOrderId = order.Id, ArticleNumber = "A1", Quantity = 1,
                Status = PartRequisitionStatus.Offen, Priority = PartRequisitionPriority.Normal,
                EmailSentAt = null,
                CreatedAt = DateTime.UtcNow, CreatedBy = "a", CreatedByWindows = "a"
            },
            new PartRequisition
            {
                ProductionOrderId = order.Id, ArticleNumber = "A2", Quantity = 1,
                Status = PartRequisitionStatus.Offen, Priority = PartRequisitionPriority.Normal,
                EmailSentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow, CreatedBy = "a", CreatedByWindows = "a"
            }
        );
        await context.SaveChangesAsync();

        var unsent = await repo.GetUnsentAsync();
        unsent.Should().HaveCount(1);
        unsent[0].ArticleNumber.Should().Be("A1");
    }
}
```

- [ ] **Step 2: Tests ausführen**

Run: `dotnet test IdealAkeWms.Tests/ --filter "PartRequisitionRepositoryTests"`
Expected: Alle Tests PASS

- [ ] **Step 3: OrderRecipientRepositoryTests erstellen**

```csharp
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Repositories;

public class OrderRecipientRepositoryTests
{
    private static (ApplicationDbContext context, OrderRecipientRepository repo) CreateRepo()
    {
        var context = TestDbContextFactory.CreateContext();
        var repo = new OrderRecipientRepository(context);
        return (context, repo);
    }

    [Fact]
    public async Task AddGroup_And_GetAllGroups()
    {
        var (context, repo) = CreateRepo();

        var group = new OrderRecipientGroup
        {
            Name = "Blechfertigung",
            Description = "Test",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        await repo.AddGroupAsync(group);

        var all = await repo.GetAllGroupsAsync();
        all.Should().HaveCount(1);
        all[0].Name.Should().Be("Blechfertigung");
    }

    [Fact]
    public async Task GetGroupsByArticleGroupAsync_ReturnsMatchingGroups()
    {
        var (context, repo) = CreateRepo();

        var group1 = new OrderRecipientGroup { Name = "Blech", CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t" };
        var group2 = new OrderRecipientGroup { Name = "Lager", CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t" };
        context.OrderRecipientGroups.AddRange(group1, group2);
        await context.SaveChangesAsync();

        context.ArticleGroupRecipientMappings.Add(new ArticleGroupRecipientMapping
        {
            ArticleGroup = "960", OrderRecipientGroupId = group1.Id,
            CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t"
        });
        await context.SaveChangesAsync();

        var result = await repo.GetGroupsByArticleGroupAsync("960");
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Blech");
    }

    [Fact]
    public async Task DeleteGroupAsync_WithOpenRequisitions_ReturnsFalse_WhenCheckedFirst()
    {
        var (context, repo) = CreateRepo();

        var group = new OrderRecipientGroup { Name = "Test", CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t" };
        context.OrderRecipientGroups.Add(group);

        var order = new ProductionOrder
        {
            OrderNumber = "WA1", Quantity = 1,
            CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t"
        };
        context.ProductionOrders.Add(order);
        await context.SaveChangesAsync();

        context.PartRequisitions.Add(new PartRequisition
        {
            ProductionOrderId = order.Id, ArticleNumber = "A1", Quantity = 1,
            Status = PartRequisitionStatus.Offen, Priority = PartRequisitionPriority.Normal,
            OrderRecipientGroupId = group.Id,
            CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t"
        });
        await context.SaveChangesAsync();

        var hasOpen = await repo.GroupHasOpenRequisitionsAsync(group.Id);
        hasOpen.Should().BeTrue();
    }

    [Fact]
    public async Task SetMappingsForArticleGroupAsync_ReplacesExisting()
    {
        var (context, repo) = CreateRepo();

        var group1 = new OrderRecipientGroup { Name = "G1", CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t" };
        var group2 = new OrderRecipientGroup { Name = "G2", CreatedAt = DateTime.UtcNow, CreatedBy = "t", CreatedByWindows = "t" };
        context.OrderRecipientGroups.AddRange(group1, group2);
        await context.SaveChangesAsync();

        await repo.SetMappingsForArticleGroupAsync("960", new List<int> { group1.Id }, "u", "w");
        var mappings = await repo.GetMappingsAsync();
        mappings.Where(m => m.ArticleGroup == "960").Should().HaveCount(1);

        await repo.SetMappingsForArticleGroupAsync("960", new List<int> { group1.Id, group2.Id }, "u", "w");
        mappings = await repo.GetMappingsAsync();
        mappings.Where(m => m.ArticleGroup == "960").Should().HaveCount(2);
    }
}
```

- [ ] **Step 4: Tests ausführen**

Run: `dotnet test IdealAkeWms.Tests/ --filter "OrderRecipientRepositoryTests"`
Expected: Alle Tests PASS

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms.Tests/Repositories/PartRequisitionRepositoryTests.cs IdealAkeWms.Tests/Repositories/OrderRecipientRepositoryTests.cs
git commit -m "test: add repository tests for part requisitions and order recipients"
```

---

## Task 6: ViewModels

**Files:**
- Create: `IdealAkeWms/Models/ViewModels/OrderRecipientViewModels.cs`
- Create: `IdealAkeWms/Models/ViewModels/PartRequisitionViewModels.cs`

- [ ] **Step 1: OrderRecipientViewModels erstellen**

```csharp
using System.ComponentModel.DataAnnotations;
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class OrderRecipientGroupViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Beschreibung")]
    public string? Description { get; set; }

    public List<OrderRecipientEditModel> Recipients { get; set; } = new();
}

public class OrderRecipientEditModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-Mail ist erforderlich")]
    [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse")]
    [StringLength(300)]
    [Display(Name = "E-Mail")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;
}

public class ArticleGroupMappingViewModel
{
    public string ArticleGroup { get; set; } = string.Empty;
    public List<int> SelectedGroupIds { get; set; } = new();
}

public class ArticleGroupMappingsPageViewModel
{
    public List<ArticleGroupMappingViewModel> Mappings { get; set; } = new();
    public List<OrderRecipientGroup> AvailableGroups { get; set; } = new();
}
```

- [ ] **Step 2: PartRequisitionViewModels erstellen**

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

// Bestellübersicht
public class PartRequisitionIndexViewModel
{
    public List<PartRequisitionListItem> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool ShowAll { get; set; }
    public string? SearchTerm { get; set; }
}

public class PartRequisitionListItem
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public int ProductionOrderId { get; set; }
    public string? Customer { get; set; }
    public string ArticleNumber { get; set; } = string.Empty;
    public string? ArticleDescription { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EmailSentAt { get; set; }
    public string? Notes { get; set; }
}

// API: Bestellung erstellen (aus BOM-Modal)
public class CreatePartRequisitionRequest
{
    public int ProductionOrderId { get; set; }
    public List<CreatePartRequisitionItem> Items { get; set; } = new();
    public string Priority { get; set; } = PartRequisitionPriority.Normal;
    public string? Notes { get; set; }
    public List<string> SelectedEmails { get; set; } = new();
}

public class CreatePartRequisitionItem
{
    public string ArticleNumber { get; set; } = string.Empty;
    public string? ArticleDescription { get; set; }
    public string? ArticleGroup { get; set; }
    public string? Position { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
}

// API: Empfänger für Artikelgruppe laden
public class RecipientGroupInfo
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<RecipientInfo> Recipients { get; set; } = new();
}

public class RecipientInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Wareneingang: offene Bedarfsmeldungen
public class OpenRequisitionForInbound
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
    public string Priority { get; set; } = string.Empty;
}
```

- [ ] **Step 3: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Erfolgreich

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Models/ViewModels/OrderRecipientViewModels.cs IdealAkeWms/Models/ViewModels/PartRequisitionViewModels.cs
git commit -m "feat: add view models for order recipients and part requisitions"
```

---

## Task 7: Access Filter + OrderRecipientsController (Stammdaten)

**Files:**
- Create: `IdealAkeWms/Filters/RequirePickingOrStockAccessAttribute.cs`
- Create: `IdealAkeWms/Controllers/OrderRecipientsController.cs`
- Create: `IdealAkeWms/Views/OrderRecipients/Index.cshtml`
- Create: `IdealAkeWms/Views/OrderRecipients/Create.cshtml`
- Create: `IdealAkeWms/Views/OrderRecipients/Edit.cshtml`
- Create: `IdealAkeWms/Views/OrderRecipients/ArticleGroupMappings.cshtml`

- [ ] **Step 1: RequirePickingOrStockAccessAttribute erstellen**

```csharp
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

public class RequirePickingOrStockAccessAttribute : TypeFilterAttribute
{
    public RequirePickingOrStockAccessAttribute() : base(typeof(RequirePickingOrStockAccessFilter)) { }
}

public class RequirePickingOrStockAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequirePickingOrStockAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanPickAsync() && !await _currentUserService.CanAccessStockAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }
}
```

- [ ] **Step 2: OrderRecipientsController erstellen**

Dieser Controller ist umfangreich. Erstelle ihn mit CRUD für Gruppen, Empfänger-Management innerhalb von Gruppen, und Artikelgruppen-Zuordnung. Folge dem Pattern der bestehenden Controller (z.B. `RolesController.cs`, `SettingsController.cs`).

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireMasterDataAccess]
public class OrderRecipientsController : Controller
{
    private readonly IOrderRecipientRepository _recipientRepository;
    private readonly ICurrentUserService _currentUserService;

    public OrderRecipientsController(
        IOrderRecipientRepository recipientRepository,
        ICurrentUserService currentUserService)
    {
        _recipientRepository = recipientRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index()
    {
        var groups = await _recipientRepository.GetAllGroupsAsync();
        return View(groups);
    }

    public IActionResult Create()
    {
        return View(new OrderRecipientGroupViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderRecipientGroupViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var group = new OrderRecipientGroup
        {
            Name = vm.Name,
            Description = vm.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _recipientRepository.AddGroupAsync(group);
        TempData["SuccessMessage"] = $"Empfängergruppe '{group.Name}' erstellt.";
        return RedirectToAction(nameof(Edit), new { id = group.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var group = await _recipientRepository.GetGroupByIdAsync(id);
        if (group == null) return NotFound();

        var vm = new OrderRecipientGroupViewModel
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            Recipients = group.Recipients.OrderBy(r => r.Name).Select(r => new OrderRecipientEditModel
            {
                Id = r.Id,
                Name = r.Name,
                Email = r.Email,
                IsActive = r.IsActive
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OrderRecipientGroupViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var group = await _recipientRepository.GetGroupByIdAsync(id);
        if (group == null) return NotFound();

        group.Name = vm.Name;
        group.Description = vm.Description;
        group.ModifiedAt = DateTime.UtcNow;
        group.ModifiedBy = _currentUserService.GetDisplayName();
        group.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _recipientRepository.UpdateGroupAsync(group);
        TempData["SuccessMessage"] = $"Empfängergruppe '{group.Name}' gespeichert.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _recipientRepository.GroupHasOpenRequisitionsAsync(id))
        {
            TempData["WarningMessage"] = "Gruppe kann nicht gelöscht werden: offene Bedarfsmeldungen vorhanden.";
            return RedirectToAction(nameof(Index));
        }

        var deleted = await _recipientRepository.DeleteGroupAsync(id);
        if (deleted)
            TempData["SuccessMessage"] = "Empfängergruppe gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    // --- Empfänger ---

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRecipient(int groupId, OrderRecipientEditModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["WarningMessage"] = "Bitte Name und E-Mail angeben.";
            return RedirectToAction(nameof(Edit), new { id = groupId });
        }

        var recipient = new OrderRecipient
        {
            OrderRecipientGroupId = groupId,
            Name = model.Name,
            Email = model.Email,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _recipientRepository.AddRecipientAsync(recipient);
        TempData["SuccessMessage"] = $"Empfänger '{recipient.Name}' hinzugefügt.";
        return RedirectToAction(nameof(Edit), new { id = groupId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRecipient(int groupId, int recipientId, string name, string email, bool isActive)
    {
        var recipient = await _recipientRepository.GetRecipientByIdAsync(recipientId);
        if (recipient == null) return NotFound();

        recipient.Name = name;
        recipient.Email = email;
        recipient.IsActive = isActive;
        recipient.ModifiedAt = DateTime.UtcNow;
        recipient.ModifiedBy = _currentUserService.GetDisplayName();
        recipient.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _recipientRepository.UpdateRecipientAsync(recipient);
        TempData["SuccessMessage"] = $"Empfänger '{recipient.Name}' gespeichert.";
        return RedirectToAction(nameof(Edit), new { id = groupId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRecipient(int groupId, int recipientId)
    {
        await _recipientRepository.DeleteRecipientAsync(recipientId);
        TempData["SuccessMessage"] = "Empfänger gelöscht.";
        return RedirectToAction(nameof(Edit), new { id = groupId });
    }

    // --- Artikelgruppen-Zuordnung ---

    public async Task<IActionResult> ArticleGroupMappings()
    {
        var articleGroups = await _recipientRepository.GetDistinctArticleGroupsAsync();
        var allMappings = await _recipientRepository.GetMappingsAsync();
        var allGroups = await _recipientRepository.GetAllGroupsAsync();

        var vm = new ArticleGroupMappingsPageViewModel
        {
            AvailableGroups = allGroups,
            Mappings = articleGroups.Select(ag => new ArticleGroupMappingViewModel
            {
                ArticleGroup = ag,
                SelectedGroupIds = allMappings
                    .Where(m => m.ArticleGroup == ag)
                    .Select(m => m.OrderRecipientGroupId)
                    .ToList()
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveArticleGroupMappings(ArticleGroupMappingsPageViewModel vm)
    {
        var displayName = _currentUserService.GetDisplayName();
        var windowsUser = _currentUserService.GetWindowsUserName();

        foreach (var mapping in vm.Mappings)
        {
            await _recipientRepository.SetMappingsForArticleGroupAsync(
                mapping.ArticleGroup,
                mapping.SelectedGroupIds ?? new List<int>(),
                displayName, windowsUser);
        }

        TempData["SuccessMessage"] = "Artikelgruppen-Zuordnungen gespeichert.";
        return RedirectToAction(nameof(ArticleGroupMappings));
    }
}
```

- [ ] **Step 3: Views erstellen**

Erstelle die 4 View-Dateien im bestehenden Layout-Stil (Bootstrap 5, AKE Corporate Design). Referenziere die bestehenden Views (`Views/Settings/Index.cshtml`, `Views/Roles/Index.cshtml`) als Vorlage für Tabellen, Formulare, Cards.

Wichtige Punkte für die Views:
- `Index.cshtml`: Tabelle mit Name, Beschreibung, Anzahl Empfänger, Bearbeiten/Löschen-Buttons
- `Create.cshtml`: Einfaches Formular (Name, Beschreibung)
- `Edit.cshtml`: Gruppen-Formular + Empfänger-Tabelle darunter (Name, E-Mail, Aktiv-Toggle, Speichern/Löschen pro Zeile) + Formular zum Hinzufügen
- `ArticleGroupMappings.cshtml`: Tabelle aller Artikelgruppen mit Multi-Checkbox pro Empfängergruppe

- [ ] **Step 4: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Erfolgreich

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Filters/RequirePickingOrStockAccessAttribute.cs IdealAkeWms/Controllers/OrderRecipientsController.cs IdealAkeWms/Views/OrderRecipients/
git commit -m "feat: add order recipients management (groups, recipients, article group mappings)"
```

---

## Task 8: PartRequisitionsController + Bestellübersicht-View

**Files:**
- Create: `IdealAkeWms/Controllers/PartRequisitionsController.cs`
- Create: `IdealAkeWms/Views/PartRequisitions/Index.cshtml`

- [ ] **Step 1: PartRequisitionsController erstellen**

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequirePickingOrStockAccess]
public class PartRequisitionsController : Controller
{
    private readonly IPartRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUserService;

    public PartRequisitionsController(
        IPartRequisitionRepository requisitionRepository,
        ICurrentUserService currentUserService)
    {
        _requisitionRepository = requisitionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index(int page = 1, bool showAll = false, string? searchTerm = null)
    {
        const int pageSize = 25;
        var (items, totalCount) = await _requisitionRepository.GetPagedAsync(page, pageSize, showAll, searchTerm);

        var vm = new PartRequisitionIndexViewModel
        {
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            TotalCount = totalCount,
            ShowAll = showAll,
            SearchTerm = searchTerm,
            Items = items.Select(r => new PartRequisitionListItem
            {
                Id = r.Id,
                Status = r.Status,
                Priority = r.Priority,
                OrderNumber = r.ProductionOrder.OrderNumber,
                ProductionOrderId = r.ProductionOrderId,
                Customer = r.ProductionOrder.Customer,
                ArticleNumber = r.ArticleNumber,
                ArticleDescription = r.ArticleDescription,
                Quantity = r.Quantity,
                Unit = r.Unit,
                CreatedBy = r.CreatedBy,
                CreatedAt = r.CreatedAt,
                EmailSentAt = r.EmailSentAt,
                Notes = r.Notes
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, int page = 1, bool showAll = false)
    {
        var canPick = await _currentUserService.CanPickAsync();
        if (!canPick)
        {
            TempData["WarningMessage"] = "Keine Berechtigung zum Stornieren.";
            return RedirectToAction(nameof(Index), new { page, showAll });
        }

        await _requisitionRepository.CancelAsync(
            id,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        TempData["SuccessMessage"] = "Bedarfsmeldung storniert.";
        return RedirectToAction(nameof(Index), new { page, showAll });
    }
}
```

- [ ] **Step 2: Bestellübersicht View erstellen**

`Views/PartRequisitions/Index.cshtml`: Tabelle mit Status-/Priorität-Badges, Spaltenfilter, Pagination, Stornieren-Button. Referenziere `Views/Tracking/OseonIndex.cshtml` für Pagination-Pattern und `Views/StockMovements/Index.cshtml` für Tabellen-Layout.

Wichtige UI-Elemente:
- Status-Badges: orange (`bg-warning`)=Offen, grün (`bg-success`)=Erfüllt, grau (`bg-secondary`)=Storniert
- Priorität-Badges: rot (`bg-danger`)=Eilt, gelb (`bg-warning text-dark`)=Dringend, kein Badge=Normal
- Toggle "Alle anzeigen" (inkl. Erledigte/Stornierte)
- Suchfeld
- Pagination (Vorherige/Nächste + Seitenzahlen)
- Stornieren-Button mit Bestätigungsdialog

- [ ] **Step 3: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Erfolgreich

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Controllers/PartRequisitionsController.cs IdealAkeWms/Views/PartRequisitions/
git commit -m "feat: add part requisitions overview page with pagination and cancel"
```

---

## Task 9: API-Controller für BOM-Modal

**Files:**
- Create: `IdealAkeWms/Controllers/Api/PartRequisitionsApiController.cs`

- [ ] **Step 1: API-Controller erstellen**

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers.Api;

[ApiController]
[Route("api/partrequisitions")]
[RequirePickingAccess]
public class PartRequisitionsApiController : ControllerBase
{
    private readonly IPartRequisitionRepository _requisitionRepository;
    private readonly IOrderRecipientRepository _recipientRepository;
    private readonly ICurrentUserService _currentUserService;

    public PartRequisitionsApiController(
        IPartRequisitionRepository requisitionRepository,
        IOrderRecipientRepository recipientRepository,
        ICurrentUserService currentUserService)
    {
        _requisitionRepository = requisitionRepository;
        _recipientRepository = recipientRepository;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Empfänger für eine Artikelgruppe laden (für Modal-Anzeige)
    /// </summary>
    [HttpGet("recipients")]
    public async Task<IActionResult> GetRecipients([FromQuery] string articleGroup)
    {
        if (string.IsNullOrWhiteSpace(articleGroup))
            return Ok(new List<RecipientGroupInfo>());

        var groups = await _recipientRepository.GetGroupsByArticleGroupAsync(articleGroup);

        var result = groups.Select(g => new RecipientGroupInfo
        {
            GroupId = g.Id,
            GroupName = g.Name,
            Recipients = g.Recipients.Select(r => new RecipientInfo
            {
                Id = r.Id,
                Name = r.Name,
                Email = r.Email
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Prüfen ob bereits offene Bedarfsmeldung existiert
    /// </summary>
    [HttpGet("check")]
    public async Task<IActionResult> CheckExisting([FromQuery] int productionOrderId, [FromQuery] string articleNumber)
    {
        var hasOpen = await _requisitionRepository.HasOpenRequisitionAsync(productionOrderId, articleNumber);
        if (!hasOpen) return Ok(new { exists = false });

        var openItems = await _requisitionRepository.GetOpenByArticleNumberAsync(articleNumber);
        var match = openItems.FirstOrDefault(r => r.ProductionOrderId == productionOrderId);
        return Ok(new
        {
            exists = true,
            createdBy = match?.CreatedBy,
            createdAt = match?.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
            quantity = match?.Quantity
        });
    }

    /// <summary>
    /// Bedarfsmeldung(en) erstellen
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreatePartRequisitionRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest("Keine Artikel ausgewählt.");

        var sentToEmails = string.Join(",", request.SelectedEmails.Where(e => !string.IsNullOrWhiteSpace(e)));
        var displayName = _currentUserService.GetDisplayName();
        var windowsUser = _currentUserService.GetWindowsUserName();

        var requisitions = request.Items.Select(item => new PartRequisition
        {
            ProductionOrderId = request.ProductionOrderId,
            ArticleNumber = item.ArticleNumber,
            ArticleDescription = item.ArticleDescription,
            ArticleGroup = item.ArticleGroup,
            Position = item.Position,
            Quantity = item.Quantity,
            Unit = item.Unit,
            Status = PartRequisitionStatus.Offen,
            Priority = request.Priority,
            Notes = request.Notes,
            SentToEmails = string.IsNullOrEmpty(sentToEmails) ? null : sentToEmails,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = displayName,
            CreatedByWindows = windowsUser
        }).ToList();

        await _requisitionRepository.AddRangeAsync(requisitions);

        return Ok(new { count = requisitions.Count });
    }

    /// <summary>
    /// Einzelne Bedarfsmeldung stornieren (AJAX)
    /// </summary>
    [HttpPost("cancel/{id}")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _requisitionRepository.CancelAsync(
            id,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        return Ok();
    }
}
```

- [ ] **Step 2: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Erfolgreich

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/Controllers/Api/PartRequisitionsApiController.cs
git commit -m "feat: add API controller for part requisition creation and recipient lookup"
```

---

## Task 10: BOM-View erweitern (Bestell-Buttons + Modal)

**Files:**
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs:180` — Bedarfsmeldungen + BestellungenAktiv laden
- Modify: `IdealAkeWms/Views/ProductionOrders/Bom.cshtml` — Bestell-Spalte + Modal + Badge + JS

- [ ] **Step 1: Controller-Action erweitern**

In `ProductionOrdersController.Bom()` (nach dem Laden der `viewItems`-Liste, vor `return View(vm)`):

1. `BestellungenAktiv`-Setting laden und als `ViewBag.BestellungenAktiv` setzen
2. Offene Bedarfsmeldungen für diesen WA laden: `var openRequisitions = await _partRequisitionRepository.GetByProductionOrderAsync(id);` und als `ViewBag.OpenRequisitions` setzen
3. `IPartRequisitionRepository` per Konstruktor-Injection hinzufügen

- [ ] **Step 2: BOM-View erweitern**

In `Bom.cshtml`:

1. **Neue Spalte "Bestellen"** in der Tabelle (nach Quell-Lagerplatz, nur wenn `BestellungenAktiv`)
   - Oranges Badge-Icon wenn offene Bedarfsmeldung existiert (Tooltip: "Bestellt am ... von ...")
   - Bestell-Button (Warenkorb-Icon) pro Zeile

2. **Sammelbestellungs-Button** im Action-Bereich (neben Transfer + Print)

3. **Bootstrap-Modal** für Einzel-/Sammelbestellung:
   - Artikel-Info (readonly)
   - Menge (editierbar)
   - Empfänger-Checkboxen (per AJAX geladen via `/api/partrequisitions/recipients?articleGroup=...`)
   - Priorität-Radio-Buttons (Normal/Dringend/Eilt)
   - Bemerkung (Textarea)
   - Doppelbestellungs-Warnung (per AJAX via `/api/partrequisitions/check`)
   - Senden-Button

4. **JavaScript** am Ende der View:
   - Modal öffnen bei Klick auf Bestell-Button
   - AJAX-Calls für Empfänger laden + Doppelbestellungsprüfung
   - Sammelbestellung: markierte Zeilen in Modal-Tabelle übernehmen
   - POST an `/api/partrequisitions/create`
   - Bei Erfolg: Seite neu laden (Badge wird sichtbar)

- [ ] **Step 3: Build + manueller Test**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Erfolgreich

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Controllers/ProductionOrdersController.cs IdealAkeWms/Views/ProductionOrders/Bom.cshtml
git commit -m "feat: add order buttons, modal and badges to BOM view"
```

---

## Task 11: Wareneingang-Integration

**Files:**
- Modify: `IdealAkeWms/Controllers/StockMovementsController.cs:73` — GET: offene Meldungen laden
- Modify: `IdealAkeWms/Controllers/StockMovementsController.cs:85` — POST: Meldungen erfüllen
- Modify: `IdealAkeWms/Views/StockMovements/Inbound.cshtml` — Offene Bedarfsmeldungen anzeigen

- [ ] **Step 1: StockMovementsController erweitern**

1. `IPartRequisitionRepository` per Konstruktor-Injection hinzufügen
2. `IAppSettingRepository` hinzufügen (für `BestellungenAktiv`-Check)

**GET Inbound** erweitern: `ViewBag.BestellungenAktiv` setzen

**POST Inbound** erweitern: Nach erfolgreichem Speichern der `StockMovement`, die angehakten Requisition-IDs aus dem Formular lesen und für jede `FulfillAsync()` aufrufen:

```csharp
// Nach: await _stockMovementRepository.AddAsync(movement);
if (fulfilledRequisitionIds != null)
{
    foreach (var reqId in fulfilledRequisitionIds)
    {
        await _partRequisitionRepository.FulfillAsync(
            reqId, movement.Id,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());
    }
}
```

Parameter `fulfilledRequisitionIds` als `List<int>?` zum POST-Action hinzufügen.

- [ ] **Step 2: Inbound.cshtml erweitern**

Nach dem Formular (vor `@section Scripts`): Einen `<div id="openRequisitions">` Bereich hinzufügen.

Per JavaScript: Wenn ein Artikel ausgewählt wird (Select2 `change`-Event), AJAX-Call an `/api/partrequisitions/open?articleNumber={number}` (neuer API-Endpoint, oder client-seitig über das bestehende ViewModel).

Alternative (einfacher): Die offenen Meldungen per AJAX nach Artikelauswahl laden:

1. Neuer API-Endpoint `GET /api/partrequisitions/open?articleNumber=X` im `PartRequisitionsApiController`
2. JavaScript in `Inbound.cshtml`: Bei Select2 `select2:select` Event → AJAX → Tabelle rendern
3. Tabelle: Checkbox (mit `name="fulfilledRequisitionIds"` + `value="{id}"`), WA-Nummer, Menge, Besteller, Datum, Bemerkung, Priorität
4. Die Checkboxen werden als `List<int> fulfilledRequisitionIds` im POST mitgeschickt

- [ ] **Step 3: API-Endpoint für offene Meldungen hinzufügen**

In `PartRequisitionsApiController.cs`:

```csharp
[HttpGet("open")]
[RequireStockAccess]
public async Task<IActionResult> GetOpenByArticle([FromQuery] string articleNumber)
{
    if (string.IsNullOrWhiteSpace(articleNumber)) return Ok(new List<OpenRequisitionForInbound>());

    var open = await _requisitionRepository.GetOpenByArticleNumberAsync(articleNumber);
    var result = open.Select(r => new OpenRequisitionForInbound
    {
        Id = r.Id,
        OrderNumber = r.ProductionOrder.OrderNumber,
        Quantity = r.Quantity,
        Unit = r.Unit,
        CreatedBy = r.CreatedBy,
        CreatedAt = r.CreatedAt,
        Notes = r.Notes,
        Priority = r.Priority
    }).ToList();

    return Ok(result);
}
```

- [ ] **Step 4: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Erfolgreich

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Controllers/StockMovementsController.cs IdealAkeWms/Views/StockMovements/Inbound.cshtml IdealAkeWms/Controllers/Api/PartRequisitionsApiController.cs
git commit -m "feat: show open part requisitions in inbound view and fulfill on booking"
```

---

## Task 12: E-Mail-Versand (Service)

**Files:**
- Create: `IDEALAKEWMSService/Services/IPartRequisitionEmailService.cs`
- Create: `IDEALAKEWMSService/Services/PartRequisitionEmailService.cs`
- Modify: `IDEALAKEWMSService/Workers/SyncWorker.cs` — Aufruf einbauen
- Modify: `IDEALAKEWMSService/Program.cs` — DI registrieren

- [ ] **Step 1: Interface erstellen**

```csharp
namespace IDEALAKEWMSService.Services;

public interface IPartRequisitionEmailService
{
    Task<int> SendPendingEmailsAsync(bool dryRun, CancellationToken ct = default);
}
```

- [ ] **Step 2: PartRequisitionEmailService implementieren**

```csharp
using Microsoft.Data.SqlClient;
using System.Text;

namespace IDEALAKEWMSService.Services;

public class PartRequisitionEmailService : IPartRequisitionEmailService
{
    private readonly IConfiguration _configuration;
    private readonly IMailService _mailService;
    private readonly ILogger<PartRequisitionEmailService> _logger;

    public PartRequisitionEmailService(
        IConfiguration configuration,
        IMailService mailService,
        ILogger<PartRequisitionEmailService> logger)
    {
        _configuration = configuration;
        _mailService = mailService;
        _logger = logger;
    }

    public async Task<int> SendPendingEmailsAsync(bool dryRun, CancellationToken ct = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");

        // Offene, ungesendete Meldungen laden
        var requisitions = await LoadUnsentRequisitionsAsync(connectionString, ct);
        if (requisitions.Count == 0) return 0;

        // Gruppieren nach SentToEmails + ProductionOrderId
        var groups = requisitions
            .GroupBy(r => new { r.SentToEmails, r.ProductionOrderId })
            .ToList();

        int sentCount = 0;
        foreach (var group in groups)
        {
            var items = group.ToList();
            var emails = (group.Key.SentToEmails ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (emails.Count == 0)
            {
                _logger.LogWarning("Bedarfsmeldungen {Ids} haben keine Empfänger — übersprungen.",
                    string.Join(", ", items.Select(i => i.Id)));
                continue;
            }

            var highestPriority = GetHighestPriority(items.Select(i => i.Priority));
            var subject = BuildSubject(highestPriority, items[0].OrderNumber);
            var htmlBody = BuildHtmlBody(items);

            if (dryRun)
            {
                _logger.LogInformation("[DryRun] Mail '{Subject}' an {Emails} — {Count} Teile",
                    subject, group.Key.SentToEmails, items.Count);
            }
            else
            {
                try
                {
                    await _mailService.SendAsync(subject, htmlBody, emails, ct);
                    await MarkAsSentAsync(connectionString, items.Select(i => i.Id).ToList(), ct);
                    sentCount += items.Count;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Versand der Bedarfsmeldung(en) {Ids}",
                        string.Join(", ", items.Select(i => i.Id)));
                }
            }
        }

        return sentCount;
    }

    private string BuildSubject(string priority, string orderNumber)
    {
        var prefix = priority switch
        {
            "Eilt" => "[EILT] ",
            "Dringend" => "[DRINGEND] ",
            _ => ""
        };
        return $"{prefix}Bedarfsmeldung — WA {orderNumber}";
    }

    private string GetHighestPriority(IEnumerable<string> priorities)
    {
        if (priorities.Any(p => p == "Eilt")) return "Eilt";
        if (priorities.Any(p => p == "Dringend")) return "Dringend";
        return "Normal";
    }

    private string BuildHtmlBody(List<RequisitionEmailRow> items)
    {
        var first = items[0];
        var sb = new StringBuilder();

        sb.Append("""
            <!DOCTYPE html>
            <html><head><meta charset="utf-8"></head>
            <body style="font-family: Arial, sans-serif; margin: 0; padding: 0;">
            <div style="background-color: #053153; padding: 20px; text-align: center;">
                <h1 style="color: white; margin: 0; font-size: 22px;">IDEAL AKE WMS — Bedarfsmeldung</h1>
            </div>
            <div style="padding: 20px; max-width: 700px; margin: 0 auto;">
            """);

        sb.Append($"""
            <h2 style="color: #053153; border-bottom: 2px solid #43A6E2; padding-bottom: 8px;">
                Werkstattauftrag {first.OrderNumber}
            </h2>
            <table style="margin-bottom: 20px;">
                <tr><td style="padding: 4px 16px 4px 0; font-weight: bold;">Kunde:</td><td>{first.Customer ?? "—"}</td></tr>
                <tr><td style="padding: 4px 16px 4px 0; font-weight: bold;">Artikelbezeichnung:</td><td>{first.OrderDescription ?? "—"}</td></tr>
                <tr><td style="padding: 4px 16px 4px 0; font-weight: bold;">Produktionsdatum:</td><td>{first.ProductionDate?.ToString("dd.MM.yyyy") ?? "—"}</td></tr>
                <tr><td style="padding: 4px 16px 4px 0; font-weight: bold;">Lieferdatum:</td><td>{first.DeliveryDate?.ToString("dd.MM.yyyy") ?? "—"}</td></tr>
            </table>
            """);

        sb.Append("""
            <table style="width: 100%; border-collapse: collapse; margin-bottom: 20px;">
            <thead>
                <tr style="background-color: #43A6E2; color: white;">
                    <th style="padding: 8px; text-align: left; border: 1px solid #ddd;">Ressourcen-Nr</th>
                    <th style="padding: 8px; text-align: left; border: 1px solid #ddd;">Bezeichnung</th>
                    <th style="padding: 8px; text-align: right; border: 1px solid #ddd;">Menge</th>
                    <th style="padding: 8px; text-align: left; border: 1px solid #ddd;">ME</th>
                </tr>
            </thead>
            <tbody>
            """);

        foreach (var item in items)
        {
            sb.Append($"""
                <tr>
                    <td style="padding: 8px; border: 1px solid #ddd;">{item.ArticleNumber}</td>
                    <td style="padding: 8px; border: 1px solid #ddd;">{item.ArticleDescription ?? "—"}</td>
                    <td style="padding: 8px; border: 1px solid #ddd; text-align: right;">{item.Quantity:N3}</td>
                    <td style="padding: 8px; border: 1px solid #ddd;">{item.Unit ?? "—"}</td>
                </tr>
                """);
        }

        sb.Append("</tbody></table>");

        sb.Append($"""
            <p><strong>Bestellt von:</strong> {first.CreatedBy}</p>
            <p><strong>Zeitpunkt:</strong> {first.CreatedAt:dd.MM.yyyy, HH:mm}</p>
            """);

        if (!string.IsNullOrWhiteSpace(first.Notes))
            sb.Append($"<p><strong>Bemerkung:</strong> {first.Notes}</p>");

        sb.Append("""
            <hr style="border: 1px solid #ddd; margin-top: 30px;">
            <p style="color: #999; font-size: 12px;">IDEAL AKE WMS — Automatisch generiert</p>
            </div></body></html>
            """);

        return sb.ToString();
    }

    private async Task<List<RequisitionEmailRow>> LoadUnsentRequisitionsAsync(string connectionString, CancellationToken ct)
    {
        const string sql = """
            SELECT r.Id, r.ProductionOrderId, r.ArticleNumber, r.ArticleDescription,
                   r.Quantity, r.Unit, r.Priority, r.Notes, r.SentToEmails,
                   r.CreatedAt, r.CreatedBy,
                   po.OrderNumber, po.Customer, po.Description1,
                   po.ProductionDate, po.DeliveryDate
            FROM PartRequisitions r
            INNER JOIN ProductionOrders po ON r.ProductionOrderId = po.Id
            WHERE r.EmailSentAt IS NULL AND r.Status = 'Offen'
            ORDER BY r.CreatedAt
            """;

        var results = new List<RequisitionEmailRow>();
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.CommandTimeout = 30;
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            results.Add(new RequisitionEmailRow
            {
                Id = reader.GetInt32(0),
                ProductionOrderId = reader.GetInt32(1),
                ArticleNumber = reader.GetString(2),
                ArticleDescription = reader.IsDBNull(3) ? null : reader.GetString(3),
                Quantity = reader.GetDecimal(4),
                Unit = reader.IsDBNull(5) ? null : reader.GetString(5),
                Priority = reader.GetString(6),
                Notes = reader.IsDBNull(7) ? null : reader.GetString(7),
                SentToEmails = reader.IsDBNull(8) ? null : reader.GetString(8),
                CreatedAt = reader.GetDateTime(9),
                CreatedBy = reader.IsDBNull(10) ? null : reader.GetString(10),
                OrderNumber = reader.GetString(11),
                Customer = reader.IsDBNull(12) ? null : reader.GetString(12),
                OrderDescription = reader.IsDBNull(13) ? null : reader.GetString(13),
                ProductionDate = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
                DeliveryDate = reader.IsDBNull(15) ? null : reader.GetDateTime(15)
            });
        }

        return results;
    }

    private async Task MarkAsSentAsync(string connectionString, List<int> ids, CancellationToken ct)
    {
        var idList = string.Join(",", ids);
        var sql = $"UPDATE PartRequisitions SET EmailSentAt = GETUTCDATE() WHERE Id IN ({idList})";

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private class RequisitionEmailRow
    {
        public int Id { get; set; }
        public int ProductionOrderId { get; set; }
        public string ArticleNumber { get; set; } = string.Empty;
        public string? ArticleDescription { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? SentToEmails { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? Customer { get; set; }
        public string? OrderDescription { get; set; }
        public DateTime? ProductionDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
    }
}
```

- [ ] **Step 3: DI-Registrierung im Service**

In `IDEALAKEWMSService/Program.cs` — AddScoped für `IPartRequisitionEmailService`:

```csharp
builder.Services.AddScoped<IPartRequisitionEmailService, PartRequisitionEmailService>();
```

- [ ] **Step 4: SyncWorker erweitern**

In `SyncWorker.cs` nach dem enaio-DMS-Sync-Block (nach Zeile 89), neuen Block hinzufügen:

```csharp
// --- Bedarfsmeldungen E-Mail-Versand ---
if (_configuration.GetValue<bool>("Sync:PartRequisitionEmailEnabled", false))
{
    try
    {
        var emailService = scope.ServiceProvider.GetRequiredService<IPartRequisitionEmailService>();
        var sentCount = await emailService.SendPendingEmailsAsync(dryRun, stoppingToken);
        if (sentCount > 0)
            _logger.LogInformation("Bedarfsmeldungen: {Count} E-Mails versendet.", sentCount);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Fehler beim Versand der Bedarfsmeldungs-E-Mails.");
    }
}
```

- [ ] **Step 5: appsettings.json Toggle hinzufügen**

In `IDEALAKEWMSService/appsettings.json`, im `Sync`-Abschnitt:

```json
"PartRequisitionEmailEnabled": false
```

- [ ] **Step 6: Build prüfen**

Run: `dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj`
Expected: Erfolgreich

- [ ] **Step 7: Commit**

```bash
git add IDEALAKEWMSService/Services/IPartRequisitionEmailService.cs IDEALAKEWMSService/Services/PartRequisitionEmailService.cs IDEALAKEWMSService/Workers/SyncWorker.cs IDEALAKEWMSService/Program.cs IDEALAKEWMSService/appsettings.json
git commit -m "feat: add part requisition email service with HTML template in AKE CI"
```

---

## Task 13: Navbar + Layout-Integration

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Menüpunkt "Bestellungen" hinzufügen**

In `_Layout.cshtml`:

1. Am Beginn der Navbar-Logik (wo `canPick`, `canViewTracking` etc. geladen werden): `BestellungenAktiv`-Setting laden:
   ```csharp
   var bestellungenAktiv = (await settingRepository.GetValueAsync("BestellungenAktiv"))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
   ```

2. Menüpunkt "Bestellungen" nach dem Lager-Bereich (Bestände/Lagerbewegungen), nur wenn `bestellungenAktiv` und User `canPick || canAccessStock`:
   ```html
   @if (bestellungenAktiv && (canPick || canAccessStock))
   {
       <li class="nav-item">
           <a class="nav-link" asp-controller="PartRequisitions" asp-action="Index">Bestellungen</a>
       </li>
   }
   ```

3. Unter "Stammdaten"-Dropdown: Menüpunkt "Empfänger" hinzufügen, nur wenn `bestellungenAktiv`:
   ```html
   @if (bestellungenAktiv)
   {
       <a class="dropdown-item" asp-controller="OrderRecipients" asp-action="Index">Empfänger</a>
       <a class="dropdown-item" asp-controller="OrderRecipients" asp-action="ArticleGroupMappings">Artikelgruppen-Zuordnung</a>
   }
   ```

- [ ] **Step 2: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Erfolgreich

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/Views/Shared/_Layout.cshtml
git commit -m "feat: add navigation menu items for part requisitions and order recipients"
```

---

## Task 14: Version + Changelog + Hilfeseite + Dokumentation

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `SQL/00_FreshInstall.sql`
- Modify: `CLAUDE.md`
- Modify: `PROJECT_STATUS.md`

- [ ] **Step 1: AppVersion hochzählen (beide Projekte)**

`IdealAkeWms/AppVersion.cs`:
```csharp
public const string Version = "1.1.0";
public const string Date = "2026-04-02";
```

`IDEALAKEWMSService/AppVersion.cs`:
```csharp
public const string Version = "1.1.0";
public const string Date = "2026-04-02";
```

- [ ] **Step 2: Changelog ergänzen**

In `Views/Help/Changelog.cshtml` — neuen Card-Block **vor** dem bestehenden v1.0.0-Block einfügen:

```html
<div class="card mb-3">
    <div class="card-header" style="background-color: var(--ake-primary);">
        <strong>v1.1.0</strong>
        <span class="text-white-50 ms-2">02.04.2026</span>
    </div>
    <div class="card-body">
        <h6 class="text-primary">Neue Funktionen</h6>
        <ul>
            <li><strong>Bedarfsmeldungen aus Stückliste</strong> — Fehlteile können direkt aus der Stückliste bestellt werden (Einzel- oder Sammelbestellung)</li>
            <li><strong>E-Mail-Benachrichtigung</strong> — Bedarfsmeldungen werden automatisch per E-Mail an konfigurierbare Empfängergruppen versendet</li>
            <li><strong>Bestellübersicht</strong> — Neue Seite zur Übersicht aller Bedarfsmeldungen mit Status, Priorität und Filterung</li>
            <li><strong>Empfänger-Verwaltung</strong> — Stammdaten-Verwaltung für Empfängergruppen mit Artikelgruppen-Zuordnung</li>
            <li><strong>Wareneingang-Integration</strong> — Offene Bedarfsmeldungen werden beim Einbuchen angezeigt und können mit der Buchung verknüpft werden</li>
            <li><strong>Prioritäten</strong> — Bedarfsmeldungen können als Normal, Dringend oder Eilt markiert werden</li>
        </ul>
    </div>
</div>
```

- [ ] **Step 3: Hilfeseite ergänzen**

In `Views/Help/Index.cshtml` — neuen Abschnitt für Bedarfsmeldungen einfügen (Stil wie bestehende Abschnitte).

- [ ] **Step 4: 00_FreshInstall.sql aktualisieren**

Den Inhalt von `SQL/36_AddPartRequisitions.sql` in `SQL/00_FreshInstall.sql` an der richtigen Stelle einfügen (nach den bestehenden CREATE TABLE-Statements).

- [ ] **Step 5: CLAUDE.md ergänzen**

Neuen Abschnitt "Bedarfsmeldungen" hinzufügen mit:
- Tabellen-Übersicht (PartRequisitions, OrderRecipientGroups, OrderRecipients, ArticleGroupRecipientMappings)
- Status-Workflow (Offen → Erfüllt / Storniert)
- E-Mail-Versand-Logik
- AppSetting `BestellungenAktiv`
- Neue Dateien-Referenzen in "Wichtige Dateien"
- Neues AppSetting in der AppSettings-Tabelle

- [ ] **Step 6: PROJECT_STATUS.md aktualisieren**

Neuen Abschnitt für v1.1.0 hinzufügen mit Feature-Beschreibung.

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml IdealAkeWms/Views/Help/Index.cshtml SQL/00_FreshInstall.sql CLAUDE.md PROJECT_STATUS.md
git commit -m "docs: update version to 1.1.0, changelog, help page, and project documentation"
```

---

## Task 15: Alle Tests ausführen + Final Build

**Files:** Keine neuen Dateien

- [ ] **Step 1: Alle Tests ausführen**

Run: `dotnet test IdealAkeWms.Tests/ -v normal`
Expected: Alle Tests PASS (bestehende + neue)

- [ ] **Step 2: Vollständiger Build beider Projekte**

Run: `dotnet build IdealAkeWms.slnx`
Expected: Erfolgreich, keine Warnings

- [ ] **Step 3: Bei Fehlern beheben und erneut testen**

Falls Tests fehlschlagen oder Build-Fehler auftreten: beheben, Tests erneut laufen lassen, dann committen.

- [ ] **Step 4: Finaler Commit (nur wenn Fixes nötig waren)**

```bash
git commit -m "fix: resolve build/test issues for part requisitions feature"
```
