# Artikelkategorien & Merkmale — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add article categories (OSEON-synced + manual) and free-form article attributes (Boolean/Dropdown) with admin CRUD pages, Article Index/Edit integration, and BOM category display.

**Architecture:** Two subsystems sharing one migration: (1) ArticleCategories table with FK on Article, OSEON sync, admin CRUD. (2) EAV-pattern with ArticleAttributeDefinitions, Options, Values — admin defines attributes, values managed per article in Edit. Both visible as filterable columns in Article Index.

**Tech Stack:** ASP.NET Core 10, EF Core 10, SQL Server, xUnit + FluentAssertions + Moq

---

## Task 1: Models + DbContext + SQL Migration + EF Migration

- [ ] Create `IdealAkeWms/Models/ArticleCategory.cs`
- [ ] Create `IdealAkeWms/Models/ArticleAttributeDefinition.cs` (includes `AttributeType` enum)
- [ ] Create `IdealAkeWms/Models/ArticleAttributeOption.cs`
- [ ] Create `IdealAkeWms/Models/ArticleAttributeValue.cs`
- [ ] Add `ArticleCategoryId` FK + navigation to `IdealAkeWms/Models/Article.cs`
- [ ] Add DbSet declarations to `IdealAkeWms/Data/ApplicationDbContext.cs`
- [ ] Add entity configurations to `OnModelCreating` in `ApplicationDbContext.cs`
- [ ] Create `SQL/39_AddArticleCategoriesAndAttributes.sql`
- [ ] Update `SQL/00_FreshInstall.sql`
- [ ] Run `dotnet ef migrations add AddArticleCategoriesAndAttributes`
- [ ] Build + existing tests pass
- [ ] Commit

### New file: `IdealAkeWms/Models/ArticleCategory.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ArticleCategory : AuditableEntity
{
    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Beschreibung")]
    public string? Description { get; set; }

    [Display(Name = "OSEON Typ")]
    public int? OseonTyp { get; set; }

    [StringLength(50)]
    [Display(Name = "Quelle")]
    public string? Source { get; set; }

    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
```

### New file: `IdealAkeWms/Models/ArticleAttributeDefinition.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public enum AttributeType
{
    Boolean = 0,
    Dropdown = 1
}

public class ArticleAttributeDefinition : AuditableEntity
{
    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Typ")]
    public AttributeType AttributeType { get; set; }

    [Display(Name = "Reihenfolge")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    [StringLength(50)]
    public string? SyncSource { get; set; }

    [StringLength(200)]
    public string? SyncFieldName { get; set; }

    public ICollection<ArticleAttributeOption> Options { get; set; } = new List<ArticleAttributeOption>();
    public ICollection<ArticleAttributeValue> Values { get; set; } = new List<ArticleAttributeValue>();
}
```

### New file: `IdealAkeWms/Models/ArticleAttributeOption.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ArticleAttributeOption
{
    public int Id { get; set; }

    public int ArticleAttributeDefinitionId { get; set; }
    public ArticleAttributeDefinition ArticleAttributeDefinition { get; set; } = null!;

    [Required(ErrorMessage = "Wert ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Wert")]
    public string Value { get; set; } = string.Empty;

    [Display(Name = "Reihenfolge")]
    public int SortOrder { get; set; }
}
```

### New file: `IdealAkeWms/Models/ArticleAttributeValue.cs`

```csharp
namespace IdealAkeWms.Models;

public class ArticleAttributeValue : AuditableEntity
{
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public int ArticleAttributeDefinitionId { get; set; }
    public ArticleAttributeDefinition ArticleAttributeDefinition { get; set; } = null!;

    public bool? BooleanValue { get; set; }

    public int? SelectedOptionId { get; set; }
    public ArticleAttributeOption? SelectedOption { get; set; }
}
```

### Edit: `IdealAkeWms/Models/Article.cs`

**Old:**
```csharp
    [Display(Name = "Meldebestand")]
    public decimal? ReorderLevel { get; set; }

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
```

**New:**
```csharp
    [Display(Name = "Meldebestand")]
    public decimal? ReorderLevel { get; set; }

    [Display(Name = "Kategorie")]
    public int? ArticleCategoryId { get; set; }
    public ArticleCategory? ArticleCategory { get; set; }

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<ArticleAttributeValue> AttributeValues { get; set; } = new List<ArticleAttributeValue>();
```

### Edit: `IdealAkeWms/Data/ApplicationDbContext.cs` — DbSet declarations

**After line (existing last DbSet):**
```csharp
    public DbSet<PartRequisition> PartRequisitions => Set<PartRequisition>();
```

**Add:**
```csharp
    public DbSet<ArticleCategory> ArticleCategories => Set<ArticleCategory>();
    public DbSet<ArticleAttributeDefinition> ArticleAttributeDefinitions => Set<ArticleAttributeDefinition>();
    public DbSet<ArticleAttributeOption> ArticleAttributeOptions => Set<ArticleAttributeOption>();
    public DbSet<ArticleAttributeValue> ArticleAttributeValues => Set<ArticleAttributeValue>();
```

### Edit: `IdealAkeWms/Data/ApplicationDbContext.cs` — OnModelCreating (Article section)

**Old (Article entity config, ends with `entity.HasIndex(e => e.ArticleNumber).IsUnique();`):**
```csharp
        // Article
        modelBuilder.Entity<Article>(entity =>
        {
            entity.ToTable("Articles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ArticleNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.ArticleGroup).HasMaxLength(100);
            entity.Property(e => e.ReorderLevel).HasColumnType("decimal(18,3)");
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.ArticleNumber).IsUnique();
        });
```

**New:**
```csharp
        // Article
        modelBuilder.Entity<Article>(entity =>
        {
            entity.ToTable("Articles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ArticleNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.ArticleGroup).HasMaxLength(100);
            entity.Property(e => e.ReorderLevel).HasColumnType("decimal(18,3)");
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.ArticleNumber).IsUnique();
            entity.HasIndex(e => e.ArticleCategoryId);

            entity.HasOne(e => e.ArticleCategory)
                .WithMany(c => c.Articles)
                .HasForeignKey(e => e.ArticleCategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });
```

### Edit: `IdealAkeWms/Data/ApplicationDbContext.cs` — Add new entity configs

**After the closing `});` of the Article entity config block, add:**

```csharp
        // ArticleCategory
        modelBuilder.Entity<ArticleCategory>(entity =>
        {
            entity.ToTable("ArticleCategories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Source).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.Name).IsUnique();
        });

        // ArticleAttributeDefinition
        modelBuilder.Entity<ArticleAttributeDefinition>(entity =>
        {
            entity.ToTable("ArticleAttributeDefinitions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SyncSource).HasMaxLength(50);
            entity.Property(e => e.SyncFieldName).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.Name).IsUnique();
        });

        // ArticleAttributeOption
        modelBuilder.Entity<ArticleAttributeOption>(entity =>
        {
            entity.ToTable("ArticleAttributeOptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value).HasMaxLength(200).IsRequired();

            entity.HasIndex(e => e.ArticleAttributeDefinitionId);

            entity.HasOne(e => e.ArticleAttributeDefinition)
                .WithMany(d => d.Options)
                .HasForeignKey(e => e.ArticleAttributeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ArticleAttributeValue
        modelBuilder.Entity<ArticleAttributeValue>(entity =>
        {
            entity.ToTable("ArticleAttributeValues");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => new { e.ArticleId, e.ArticleAttributeDefinitionId }).IsUnique();
            entity.HasIndex(e => e.ArticleId);

            entity.HasOne(e => e.Article)
                .WithMany(a => a.AttributeValues)
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ArticleAttributeDefinition)
                .WithMany(d => d.Values)
                .HasForeignKey(e => e.ArticleAttributeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SelectedOption)
                .WithMany()
                .HasForeignKey(e => e.SelectedOptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });
```

### New file: `SQL/39_AddArticleCategoriesAndAttributes.sql`

```sql
-- =============================================
-- 39: ArticleCategories + ArticleAttributes (EAV)
-- =============================================

-- ArticleCategories
IF OBJECT_ID(N'[dbo].[ArticleCategories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleCategories] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [Name]              NVARCHAR(200)  NOT NULL,
        [Description]       NVARCHAR(500)  NULL,
        [OseonTyp]          INT            NULL,
        [Source]            NVARCHAR(50)   NULL,
        [CreatedAt]         DATETIME2      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [CreatedByWindows]  NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [ModifiedAt]        DATETIME2      NULL,
        [ModifiedBy]        NVARCHAR(200)  NULL,
        [ModifiedByWindows] NVARCHAR(200)  NULL,
        CONSTRAINT [PK_ArticleCategories] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ArticleCategories_Name] UNIQUE ([Name])
    );
    PRINT 'Tabelle [ArticleCategories] erstellt.';
END
GO

-- Articles: ArticleCategoryId FK
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Articles') AND name = 'ArticleCategoryId')
BEGIN
    ALTER TABLE [dbo].[Articles] ADD [ArticleCategoryId] INT NULL;

    ALTER TABLE [dbo].[Articles] ADD CONSTRAINT [FK_Articles_ArticleCategories]
        FOREIGN KEY ([ArticleCategoryId]) REFERENCES [dbo].[ArticleCategories]([Id])
        ON DELETE SET NULL;

    CREATE NONCLUSTERED INDEX [IX_Articles_ArticleCategoryId]
        ON [dbo].[Articles] ([ArticleCategoryId]);

    PRINT 'Spalte [Articles].[ArticleCategoryId] mit FK + Index erstellt.';
END
GO

-- ArticleAttributeDefinitions
IF OBJECT_ID(N'[dbo].[ArticleAttributeDefinitions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleAttributeDefinitions] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [Name]              NVARCHAR(200)  NOT NULL,
        [AttributeType]     INT            NOT NULL DEFAULT 0,
        [SortOrder]         INT            NOT NULL DEFAULT 0,
        [IsActive]          BIT            NOT NULL DEFAULT 1,
        [SyncSource]        NVARCHAR(50)   NULL,
        [SyncFieldName]     NVARCHAR(200)  NULL,
        [CreatedAt]         DATETIME2      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [CreatedByWindows]  NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [ModifiedAt]        DATETIME2      NULL,
        [ModifiedBy]        NVARCHAR(200)  NULL,
        [ModifiedByWindows] NVARCHAR(200)  NULL,
        CONSTRAINT [PK_ArticleAttributeDefinitions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ArticleAttributeDefinitions_Name] UNIQUE ([Name])
    );
    PRINT 'Tabelle [ArticleAttributeDefinitions] erstellt.';
END
GO

-- ArticleAttributeOptions
IF OBJECT_ID(N'[dbo].[ArticleAttributeOptions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleAttributeOptions] (
        [Id]                              INT            IDENTITY(1,1) NOT NULL,
        [ArticleAttributeDefinitionId]    INT            NOT NULL,
        [Value]                           NVARCHAR(200)  NOT NULL,
        [SortOrder]                       INT            NOT NULL DEFAULT 0,
        CONSTRAINT [PK_ArticleAttributeOptions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ArticleAttributeOptions_Definitions]
            FOREIGN KEY ([ArticleAttributeDefinitionId])
            REFERENCES [dbo].[ArticleAttributeDefinitions]([Id])
            ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_ArticleAttributeOptions_DefinitionId]
        ON [dbo].[ArticleAttributeOptions] ([ArticleAttributeDefinitionId]);

    PRINT 'Tabelle [ArticleAttributeOptions] erstellt.';
END
GO

-- ArticleAttributeValues
IF OBJECT_ID(N'[dbo].[ArticleAttributeValues]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleAttributeValues] (
        [Id]                              INT            IDENTITY(1,1) NOT NULL,
        [ArticleId]                       INT            NOT NULL,
        [ArticleAttributeDefinitionId]    INT            NOT NULL,
        [BooleanValue]                    BIT            NULL,
        [SelectedOptionId]                INT            NULL,
        [CreatedAt]                       DATETIME2      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]                       NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [CreatedByWindows]                NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [ModifiedAt]                      DATETIME2      NULL,
        [ModifiedBy]                      NVARCHAR(200)  NULL,
        [ModifiedByWindows]               NVARCHAR(200)  NULL,
        CONSTRAINT [PK_ArticleAttributeValues] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ArticleAttributeValues_Articles]
            FOREIGN KEY ([ArticleId])
            REFERENCES [dbo].[Articles]([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_ArticleAttributeValues_Definitions]
            FOREIGN KEY ([ArticleAttributeDefinitionId])
            REFERENCES [dbo].[ArticleAttributeDefinitions]([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_ArticleAttributeValues_Options]
            FOREIGN KEY ([SelectedOptionId])
            REFERENCES [dbo].[ArticleAttributeOptions]([Id])
            ON DELETE SET NULL
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_ArticleAttributeValues_ArticleId_DefinitionId]
        ON [dbo].[ArticleAttributeValues] ([ArticleId], [ArticleAttributeDefinitionId]);

    CREATE NONCLUSTERED INDEX [IX_ArticleAttributeValues_ArticleId]
        ON [dbo].[ArticleAttributeValues] ([ArticleId]);

    PRINT 'Tabelle [ArticleAttributeValues] erstellt.';
END
GO

-- EF Migrations History
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] LIKE '%_AddArticleCategoriesAndAttributes')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260404120000_AddArticleCategoriesAndAttributes', '10.0.0');
GO

PRINT 'Migration 39 (ArticleCategories + ArticleAttributes) abgeschlossen.';
GO
```

### Edit: `SQL/00_FreshInstall.sql`

Insert the new table definitions **before** the `-- 17. Standard-Daten` section. Find the line:

**Old:**
```sql
-- =============================================
-- 17. Standard-Daten
-- =============================================
```

**New (insert BEFORE that line):**
```sql
-- =============================================
-- 16c. ArticleCategories
-- =============================================
IF OBJECT_ID(N'[dbo].[ArticleCategories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleCategories] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [Name]              NVARCHAR(200)  NOT NULL,
        [Description]       NVARCHAR(500)  NULL,
        [OseonTyp]          INT            NULL,
        [Source]            NVARCHAR(50)   NULL,
        [CreatedAt]         DATETIME2      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [CreatedByWindows]  NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [ModifiedAt]        DATETIME2      NULL,
        [ModifiedBy]        NVARCHAR(200)  NULL,
        [ModifiedByWindows] NVARCHAR(200)  NULL,
        CONSTRAINT [PK_ArticleCategories] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ArticleCategories_Name] UNIQUE ([Name])
    );
    PRINT 'Tabelle [ArticleCategories] erstellt.';
END
GO

-- Articles: ArticleCategoryId FK
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Articles') AND name = 'ArticleCategoryId')
BEGIN
    ALTER TABLE [dbo].[Articles] ADD [ArticleCategoryId] INT NULL;
    ALTER TABLE [dbo].[Articles] ADD CONSTRAINT [FK_Articles_ArticleCategories]
        FOREIGN KEY ([ArticleCategoryId]) REFERENCES [dbo].[ArticleCategories]([Id])
        ON DELETE SET NULL;
    CREATE NONCLUSTERED INDEX [IX_Articles_ArticleCategoryId]
        ON [dbo].[Articles] ([ArticleCategoryId]);
    PRINT 'Spalte [Articles].[ArticleCategoryId] mit FK + Index erstellt.';
END
GO

-- =============================================
-- 16d. ArticleAttributeDefinitions + Options + Values
-- =============================================
IF OBJECT_ID(N'[dbo].[ArticleAttributeDefinitions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleAttributeDefinitions] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [Name]              NVARCHAR(200)  NOT NULL,
        [AttributeType]     INT            NOT NULL DEFAULT 0,
        [SortOrder]         INT            NOT NULL DEFAULT 0,
        [IsActive]          BIT            NOT NULL DEFAULT 1,
        [SyncSource]        NVARCHAR(50)   NULL,
        [SyncFieldName]     NVARCHAR(200)  NULL,
        [CreatedAt]         DATETIME2      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [CreatedByWindows]  NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [ModifiedAt]        DATETIME2      NULL,
        [ModifiedBy]        NVARCHAR(200)  NULL,
        [ModifiedByWindows] NVARCHAR(200)  NULL,
        CONSTRAINT [PK_ArticleAttributeDefinitions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ArticleAttributeDefinitions_Name] UNIQUE ([Name])
    );
    PRINT 'Tabelle [ArticleAttributeDefinitions] erstellt.';
END
GO

IF OBJECT_ID(N'[dbo].[ArticleAttributeOptions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleAttributeOptions] (
        [Id]                              INT            IDENTITY(1,1) NOT NULL,
        [ArticleAttributeDefinitionId]    INT            NOT NULL,
        [Value]                           NVARCHAR(200)  NOT NULL,
        [SortOrder]                       INT            NOT NULL DEFAULT 0,
        CONSTRAINT [PK_ArticleAttributeOptions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ArticleAttributeOptions_Definitions]
            FOREIGN KEY ([ArticleAttributeDefinitionId])
            REFERENCES [dbo].[ArticleAttributeDefinitions]([Id])
            ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ArticleAttributeOptions_DefinitionId]
        ON [dbo].[ArticleAttributeOptions] ([ArticleAttributeDefinitionId]);
    PRINT 'Tabelle [ArticleAttributeOptions] erstellt.';
END
GO

IF OBJECT_ID(N'[dbo].[ArticleAttributeValues]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ArticleAttributeValues] (
        [Id]                              INT            IDENTITY(1,1) NOT NULL,
        [ArticleId]                       INT            NOT NULL,
        [ArticleAttributeDefinitionId]    INT            NOT NULL,
        [BooleanValue]                    BIT            NULL,
        [SelectedOptionId]                INT            NULL,
        [CreatedAt]                       DATETIME2      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]                       NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [CreatedByWindows]                NVARCHAR(200)  NOT NULL DEFAULT 'system',
        [ModifiedAt]                      DATETIME2      NULL,
        [ModifiedBy]                      NVARCHAR(200)  NULL,
        [ModifiedByWindows]               NVARCHAR(200)  NULL,
        CONSTRAINT [PK_ArticleAttributeValues] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ArticleAttributeValues_Articles]
            FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ArticleAttributeValues_Definitions]
            FOREIGN KEY ([ArticleAttributeDefinitionId]) REFERENCES [dbo].[ArticleAttributeDefinitions]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ArticleAttributeValues_Options]
            FOREIGN KEY ([SelectedOptionId]) REFERENCES [dbo].[ArticleAttributeOptions]([Id]) ON DELETE SET NULL
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ArticleAttributeValues_ArticleId_DefinitionId]
        ON [dbo].[ArticleAttributeValues] ([ArticleId], [ArticleAttributeDefinitionId]);
    CREATE NONCLUSTERED INDEX [IX_ArticleAttributeValues_ArticleId]
        ON [dbo].[ArticleAttributeValues] ([ArticleId]);
    PRINT 'Tabelle [ArticleAttributeValues] erstellt.';
END
GO

-- =============================================
-- 17. Standard-Daten
-- =============================================
```

Also add to the EF Migrations History section at the end of `00_FreshInstall.sql`, before the final `GO` / `PRINT` lines:

**After:**
```sql
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260403161339_AddPickingRelease')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260403161339_AddPickingRelease', '10.0.0');
```

**Add:**
```sql
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] LIKE '%_AddArticleCategoriesAndAttributes')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260404120000_AddArticleCategoriesAndAttributes', '10.0.0');
```

### Commands

```bash
cd /c/Entwicklung/C#/AKEBDELight
dotnet ef migrations add AddArticleCategoriesAndAttributes --project IdealAkeWms --startup-project IdealAkeWms
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests
```

### Commit

```
feat: add ArticleCategory + ArticleAttribute models, DbContext config, SQL migration 39
```

---

## Task 2: ArticleCategoryRepository + Tests

- [ ] Create `IdealAkeWms/Data/Repositories/IArticleCategoryRepository.cs`
- [ ] Create `IdealAkeWms/Data/Repositories/ArticleCategoryRepository.cs`
- [ ] Register in `IdealAkeWms/Program.cs`
- [ ] Create `IdealAkeWms.Tests/Repositories/ArticleCategoryRepositoryTests.cs`
- [ ] Build + tests pass
- [ ] Commit

### New file: `IdealAkeWms/Data/Repositories/IArticleCategoryRepository.cs`

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IArticleCategoryRepository
{
    Task<List<ArticleCategory>> GetAllOrderedAsync();
    Task<ArticleCategory?> GetByIdAsync(int id);
    Task<ArticleCategory?> GetByNameAsync(string name);
    Task<Dictionary<string, int>> GetCategoryNameToIdMapAsync();
    Task<Dictionary<int, int>> GetArticleCountByCategoryAsync();
    Task AddAsync(ArticleCategory category);
    Task UpdateAsync(ArticleCategory category);
    Task DeleteAsync(int id);
    Task<bool> ExistsByNameAsync(string name);
}
```

### New file: `IdealAkeWms/Data/Repositories/ArticleCategoryRepository.cs`

```csharp
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ArticleCategoryRepository : IArticleCategoryRepository
{
    private readonly ApplicationDbContext _context;

    public ArticleCategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ArticleCategory>> GetAllOrderedAsync()
    {
        return await _context.ArticleCategories
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<ArticleCategory?> GetByIdAsync(int id)
    {
        return await _context.ArticleCategories.FindAsync(id);
    }

    public async Task<ArticleCategory?> GetByNameAsync(string name)
    {
        return await _context.ArticleCategories
            .FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<Dictionary<string, int>> GetCategoryNameToIdMapAsync()
    {
        return await _context.ArticleCategories
            .ToDictionaryAsync(c => c.Name, c => c.Id);
    }

    public async Task<Dictionary<int, int>> GetArticleCountByCategoryAsync()
    {
        return await _context.Articles
            .Where(a => a.ArticleCategoryId != null)
            .GroupBy(a => a.ArticleCategoryId!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public async Task AddAsync(ArticleCategory category)
    {
        _context.ArticleCategories.Add(category);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ArticleCategory category)
    {
        _context.ArticleCategories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _context.ArticleCategories.FindAsync(id);
        if (category != null)
        {
            _context.ArticleCategories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.ArticleCategories.AnyAsync(c => c.Name == name);
    }
}
```

### Edit: `IdealAkeWms/Program.cs` — Register repository

**After:**
```csharp
builder.Services.AddScoped<IPartRequisitionRepository, PartRequisitionRepository>();
```

**Add:**
```csharp
builder.Services.AddScoped<IArticleCategoryRepository, ArticleCategoryRepository>();
```

### New file: `IdealAkeWms.Tests/Repositories/ArticleCategoryRepositoryTests.cs`

```csharp
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class ArticleCategoryRepositoryTests
{
    private static ArticleCategory CreateCategory(string name, string? source = null, string? description = null)
        => new()
        {
            Name = name,
            Description = description,
            Source = source,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        };

    [Fact]
    public async Task GetAllOrderedAsync_ReturnsAlphabetically()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleCategories.AddRange(
            CreateCategory("Zuschnitt"),
            CreateCategory("Blechtafel_AKE"),
            CreateCategory("Lackierteile"));
        await context.SaveChangesAsync();

        var repo = new ArticleCategoryRepository(context);
        var result = await repo.GetAllOrderedAsync();

        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Blechtafel_AKE");
        result[1].Name.Should().Be("Lackierteile");
        result[2].Name.Should().Be("Zuschnitt");
    }

    [Fact]
    public async Task GetByNameAsync_FindsCorrectCategory()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleCategories.Add(CreateCategory("Blechtafel_AKE", "OSEON", "Blechtafeln"));
        await context.SaveChangesAsync();

        var repo = new ArticleCategoryRepository(context);
        var result = await repo.GetByNameAsync("Blechtafel_AKE");

        result.Should().NotBeNull();
        result!.Source.Should().Be("OSEON");
        result.Description.Should().Be("Blechtafeln");
    }

    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsNull()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new ArticleCategoryRepository(context);

        var result = await repo.GetByNameAsync("NOPE");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCategoryNameToIdMapAsync_ReturnsDictionary()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleCategories.AddRange(
            CreateCategory("Cat1"),
            CreateCategory("Cat2"));
        await context.SaveChangesAsync();

        var repo = new ArticleCategoryRepository(context);
        var map = await repo.GetCategoryNameToIdMapAsync();

        map.Should().HaveCount(2);
        map.Should().ContainKey("Cat1");
        map.Should().ContainKey("Cat2");
    }

    [Fact]
    public async Task GetArticleCountByCategoryAsync_CountsCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var cat = CreateCategory("TestCat");
        context.ArticleCategories.Add(cat);
        await context.SaveChangesAsync();

        context.Articles.AddRange(
            new Article { ArticleNumber = "A1", ArticleCategoryId = cat.Id, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" },
            new Article { ArticleNumber = "A2", ArticleCategoryId = cat.Id, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" },
            new Article { ArticleNumber = "A3", CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" });
        await context.SaveChangesAsync();

        var repo = new ArticleCategoryRepository(context);
        var counts = await repo.GetArticleCountByCategoryAsync();

        counts.Should().ContainKey(cat.Id);
        counts[cat.Id].Should().Be(2);
    }

    [Fact]
    public async Task AddAsync_PersistsCategory()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new ArticleCategoryRepository(context);

        await repo.AddAsync(CreateCategory("Neukat", "OSEON"));

        context.ArticleCategories.Should().HaveCount(1);
        var saved = context.ArticleCategories.First();
        saved.Name.Should().Be("Neukat");
        saved.Source.Should().Be("OSEON");
    }

    [Fact]
    public async Task DeleteAsync_RemovesCategory()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleCategories.Add(CreateCategory("ToDelete"));
        await context.SaveChangesAsync();

        var repo = new ArticleCategoryRepository(context);
        var cat = await repo.GetByNameAsync("ToDelete");
        await repo.DeleteAsync(cat!.Id);

        context.ArticleCategories.Should().BeEmpty();
    }

    [Fact]
    public async Task ExistsByNameAsync_ReturnsTrueForExisting()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleCategories.Add(CreateCategory("Exists"));
        await context.SaveChangesAsync();

        var repo = new ArticleCategoryRepository(context);

        (await repo.ExistsByNameAsync("Exists")).Should().BeTrue();
        (await repo.ExistsByNameAsync("Nope")).Should().BeFalse();
    }
}
```

### Commands

```bash
cd /c/Entwicklung/C#/AKEBDELight
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests
```

### Commit

```
feat: add ArticleCategoryRepository with interface, DI registration, and tests
```

---

## Task 3: ArticleCategoriesController + View

- [ ] Create `IdealAkeWms/Controllers/ArticleCategoriesController.cs`
- [ ] Create `IdealAkeWms/Views/ArticleCategories/Index.cshtml`
- [ ] Add menu entry in `IdealAkeWms/Views/Shared/_Layout.cshtml`
- [ ] Build + test
- [ ] Commit

### New file: `IdealAkeWms/Controllers/ArticleCategoriesController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataAccess]
public class ArticleCategoriesController : Controller
{
    private readonly IArticleCategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUserService;

    public ArticleCategoriesController(
        IArticleCategoryRepository categoryRepository,
        ICurrentUserService currentUserService)
    {
        _categoryRepository = categoryRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _categoryRepository.GetAllOrderedAsync();
        var articleCounts = await _categoryRepository.GetArticleCountByCategoryAsync();
        ViewBag.ArticleCounts = articleCounts;
        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["WarningMessage"] = "Name darf nicht leer sein.";
            return RedirectToAction(nameof(Index));
        }

        if (await _categoryRepository.ExistsByNameAsync(name.Trim()))
        {
            TempData["WarningMessage"] = $"Kategorie '{name.Trim()}' existiert bereits.";
            return RedirectToAction(nameof(Index));
        }

        var category = new ArticleCategory
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _categoryRepository.AddAsync(category);
        TempData["SuccessMessage"] = $"Kategorie '{name.Trim()}' erstellt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? description)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["WarningMessage"] = "Name darf nicht leer sein.";
            return RedirectToAction(nameof(Index));
        }

        // Check uniqueness (excluding self)
        var existing = await _categoryRepository.GetByNameAsync(name.Trim());
        if (existing != null && existing.Id != id)
        {
            TempData["WarningMessage"] = $"Kategorie '{name.Trim()}' existiert bereits.";
            return RedirectToAction(nameof(Index));
        }

        category.Name = name.Trim();
        category.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        category.ModifiedAt = DateTime.Now;
        category.ModifiedBy = _currentUserService.GetDisplayName();
        category.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _categoryRepository.UpdateAsync(category);
        TempData["SuccessMessage"] = $"Kategorie '{name.Trim()}' aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
            return NotFound();

        var articleCounts = await _categoryRepository.GetArticleCountByCategoryAsync();
        if (articleCounts.TryGetValue(id, out var count) && count > 0)
        {
            TempData["WarningMessage"] = $"Kategorie '{category.Name}' kann nicht geloescht werden — {count} Artikel verwenden diese Kategorie.";
            return RedirectToAction(nameof(Index));
        }

        var name = category.Name;
        var isOseon = category.Source == "OSEON";
        await _categoryRepository.DeleteAsync(id);

        var msg = $"Kategorie '{name}' geloescht.";
        if (isOseon)
            msg += " Hinweis: OSEON-Kategorie wird beim naechsten Sync ggf. neu erstellt.";

        TempData["SuccessMessage"] = msg;
        return RedirectToAction(nameof(Index));
    }
}
```

### New file: `IdealAkeWms/Views/ArticleCategories/Index.cshtml`

```html
@model List<IdealAkeWms.Models.ArticleCategory>
@{
    ViewData["Title"] = "Artikelkategorien";
    var articleCounts = ViewBag.ArticleCounts as Dictionary<int, int> ?? new Dictionary<int, int>();
}

<h2 class="page-header">Artikelkategorien</h2>
<p class="text-muted mb-3">
    Verwalten Sie Artikelkategorien. OSEON-synchronisierte Kategorien sind als solche gekennzeichnet.
</p>

<div class="row">
    <div class="col-lg-8 mb-4">
        <div class="card">
            <div class="card-header d-flex justify-content-between align-items-center flex-wrap gap-2">
                Kategorien (@Model.Count)
            </div>
            <div class="card-body p-0">
                <div class="table-responsive">
                    <table class="table table-striped mb-0">
                        <thead>
                            <tr>
                                <th>Name</th>
                                <th>Beschreibung</th>
                                <th style="width: 100px;">Quelle</th>
                                <th style="width: 80px;" class="text-center">Artikel</th>
                                <th style="width: 140px;"></th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var cat in Model)
                            {
                                articleCounts.TryGetValue(cat.Id, out var count);
                                <tr>
                                    <td>
                                        <form asp-action="Update" method="post" class="d-flex align-items-center gap-1" id="form-update-@cat.Id">
                                            @Html.AntiForgeryToken()
                                            <input type="hidden" name="id" value="@cat.Id" />
                                            <input type="text" name="name" value="@cat.Name" class="form-control form-control-sm" required />
                                        </form>
                                    </td>
                                    <td>
                                        <input type="text" name="description" value="@cat.Description" form="form-update-@cat.Id" class="form-control form-control-sm" placeholder="Beschreibung" />
                                    </td>
                                    <td>
                                        @if (cat.Source == "OSEON")
                                        {
                                            <span class="badge bg-info">OSEON</span>
                                        }
                                        else
                                        {
                                            <span class="badge bg-secondary">Manuell</span>
                                        }
                                    </td>
                                    <td class="text-center">
                                        @count
                                    </td>
                                    <td>
                                        <div class="d-flex gap-1">
                                            <button type="submit" form="form-update-@cat.Id" class="btn btn-sm btn-outline-primary" title="Speichern">
                                                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                                                    <path d="M12.736 3.97a.733.733 0 0 1 1.047 0c.286.289.29.756.01 1.05L7.88 12.01a.733.733 0 0 1-1.065.02L3.217 8.384a.757.757 0 0 1 0-1.06.733.733 0 0 1 1.047 0l3.052 3.093 5.4-6.425z"/>
                                                </svg>
                                            </button>
                                            <form asp-action="Delete" method="post" class="d-inline">
                                                @Html.AntiForgeryToken()
                                                <input type="hidden" name="id" value="@cat.Id" />
                                                <button type="submit" class="btn btn-sm btn-outline-danger" title="Loeschen"
                                                        onclick="return confirm('Kategorie \'@cat.Name\' wirklich loeschen?');">
                                                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                                                        <path d="M5.5 5.5A.5.5 0 0 1 6 6v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5m2.5 0a.5.5 0 0 1 .5.5v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5m3 .5a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0z"/>
                                                        <path d="M14.5 3a1 1 0 0 1-1 1H13v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4h-.5a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1H6a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1h3.5a1 1 0 0 1 1 1zM4.118 4 4 4.059V13a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V4.059L11.882 4zM2.5 3h11V2h-11z"/>
                                                    </svg>
                                                </button>
                                            </form>
                                        </div>
                                    </td>
                                </tr>
                            }
                            @if (!Model.Any())
                            {
                                <tr>
                                    <td colspan="5" class="text-center text-muted py-3">Keine Kategorien vorhanden.</td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>

    <div class="col-lg-4 mb-4">
        <div class="card">
            <div class="card-header">Neue Kategorie erstellen</div>
            <div class="card-body">
                <form asp-action="Create" method="post">
                    @Html.AntiForgeryToken()
                    <div class="mb-3">
                        <label class="form-label fw-bold">Name</label>
                        <input type="text" name="name" class="form-control" placeholder="z.B. Blechtafel_AKE" required />
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold">Beschreibung</label>
                        <input type="text" name="description" class="form-control" placeholder="Optionale Beschreibung" />
                    </div>
                    <button type="submit" class="btn btn-primary w-100">Erstellen</button>
                </form>
            </div>
        </div>
    </div>
</div>
```

### Edit: `IdealAkeWms/Views/Shared/_Layout.cshtml` — Add menu entry

**Old (inside the `@if (await CurrentUserService.HasMasterDataAccessAsync())` block, after Rollen):**
```html
                                    <li><a class="dropdown-item" asp-controller="Roles" asp-action="Index">Rollen</a></li>
                                    <li><hr class="dropdown-divider" style="border-color: rgba(255,255,255,0.2);" /></li>
                                    <li><a class="dropdown-item" asp-controller="Settings" asp-action="Index">Einstellungen</a></li>
```

**New:**
```html
                                    <li><a class="dropdown-item" asp-controller="Roles" asp-action="Index">Rollen</a></li>
                                    <li><a class="dropdown-item" asp-controller="ArticleCategories" asp-action="Index">Artikelkategorien</a></li>
                                    <li><a class="dropdown-item" asp-controller="ArticleAttributes" asp-action="Index">Artikelmerkmale</a></li>
                                    <li><hr class="dropdown-divider" style="border-color: rgba(255,255,255,0.2);" /></li>
                                    <li><a class="dropdown-item" asp-controller="Settings" asp-action="Index">Einstellungen</a></li>
```

### Commands

```bash
cd /c/Entwicklung/C#/AKEBDELight
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests
```

### Commit

```
feat: add ArticleCategoriesController with CRUD view and navbar menu entry
```

---

## Task 4: ArticleAttributeRepository + Tests

- [ ] Create `IdealAkeWms/Data/Repositories/IArticleAttributeRepository.cs`
- [ ] Create `IdealAkeWms/Data/Repositories/ArticleAttributeRepository.cs`
- [ ] Register in `IdealAkeWms/Program.cs`
- [ ] Create `IdealAkeWms.Tests/Repositories/ArticleAttributeRepositoryTests.cs`
- [ ] Build + tests pass
- [ ] Commit

### New file: `IdealAkeWms/Data/Repositories/IArticleAttributeRepository.cs`

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IArticleAttributeRepository
{
    // Definitions
    Task<List<ArticleAttributeDefinition>> GetAllDefinitionsAsync();
    Task<List<ArticleAttributeDefinition>> GetActiveDefinitionsOrderedAsync();
    Task<ArticleAttributeDefinition?> GetDefinitionByIdAsync(int id);
    Task AddDefinitionAsync(ArticleAttributeDefinition definition);
    Task UpdateDefinitionAsync(ArticleAttributeDefinition definition);
    Task DeleteDefinitionAsync(int id);
    Task<bool> DefinitionHasValuesAsync(int definitionId);
    Task<bool> DefinitionExistsByNameAsync(string name);
    Task<int> GetNextSortOrderAsync();

    // Options
    Task<List<ArticleAttributeOption>> GetOptionsByDefinitionIdAsync(int definitionId);
    Task AddOptionAsync(ArticleAttributeOption option);
    Task DeleteOptionAsync(int id);
    Task<bool> OptionIsInUseAsync(int optionId);

    // Values
    Task<List<ArticleAttributeValue>> GetValuesByArticleIdAsync(int articleId);
    Task<Dictionary<int, List<ArticleAttributeValue>>> GetValuesByArticleIdsAsync(List<int> articleIds);
    Task SaveValuesAsync(int articleId, List<ArticleAttributeValue> values, string userName, string windowsUser);

    // Batch for BOM
    Task<Dictionary<string, string?>> GetCategoryNamesByArticleNumbersAsync(List<string> articleNumbers);
}
```

### New file: `IdealAkeWms/Data/Repositories/ArticleAttributeRepository.cs`

```csharp
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ArticleAttributeRepository : IArticleAttributeRepository
{
    private readonly ApplicationDbContext _context;

    public ArticleAttributeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // ========== Definitions ==========

    public async Task<List<ArticleAttributeDefinition>> GetAllDefinitionsAsync()
    {
        return await _context.ArticleAttributeDefinitions
            .Include(d => d.Options.OrderBy(o => o.SortOrder))
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<List<ArticleAttributeDefinition>> GetActiveDefinitionsOrderedAsync()
    {
        return await _context.ArticleAttributeDefinitions
            .Where(d => d.IsActive)
            .Include(d => d.Options.OrderBy(o => o.SortOrder))
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<ArticleAttributeDefinition?> GetDefinitionByIdAsync(int id)
    {
        return await _context.ArticleAttributeDefinitions
            .Include(d => d.Options.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task AddDefinitionAsync(ArticleAttributeDefinition definition)
    {
        _context.ArticleAttributeDefinitions.Add(definition);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateDefinitionAsync(ArticleAttributeDefinition definition)
    {
        _context.ArticleAttributeDefinitions.Update(definition);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDefinitionAsync(int id)
    {
        var definition = await _context.ArticleAttributeDefinitions.FindAsync(id);
        if (definition != null)
        {
            _context.ArticleAttributeDefinitions.Remove(definition);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> DefinitionHasValuesAsync(int definitionId)
    {
        return await _context.ArticleAttributeValues
            .AnyAsync(v => v.ArticleAttributeDefinitionId == definitionId);
    }

    public async Task<bool> DefinitionExistsByNameAsync(string name)
    {
        return await _context.ArticleAttributeDefinitions
            .AnyAsync(d => d.Name == name);
    }

    public async Task<int> GetNextSortOrderAsync()
    {
        var max = await _context.ArticleAttributeDefinitions
            .MaxAsync(d => (int?)d.SortOrder);
        return (max ?? -1) + 1;
    }

    // ========== Options ==========

    public async Task<List<ArticleAttributeOption>> GetOptionsByDefinitionIdAsync(int definitionId)
    {
        return await _context.ArticleAttributeOptions
            .Where(o => o.ArticleAttributeDefinitionId == definitionId)
            .OrderBy(o => o.SortOrder)
            .ToListAsync();
    }

    public async Task AddOptionAsync(ArticleAttributeOption option)
    {
        _context.ArticleAttributeOptions.Add(option);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteOptionAsync(int id)
    {
        var option = await _context.ArticleAttributeOptions.FindAsync(id);
        if (option != null)
        {
            _context.ArticleAttributeOptions.Remove(option);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> OptionIsInUseAsync(int optionId)
    {
        return await _context.ArticleAttributeValues
            .AnyAsync(v => v.SelectedOptionId == optionId);
    }

    // ========== Values ==========

    public async Task<List<ArticleAttributeValue>> GetValuesByArticleIdAsync(int articleId)
    {
        return await _context.ArticleAttributeValues
            .Include(v => v.ArticleAttributeDefinition)
            .Include(v => v.SelectedOption)
            .Where(v => v.ArticleId == articleId)
            .ToListAsync();
    }

    public async Task<Dictionary<int, List<ArticleAttributeValue>>> GetValuesByArticleIdsAsync(List<int> articleIds)
    {
        if (!articleIds.Any())
            return new Dictionary<int, List<ArticleAttributeValue>>();

        var values = await _context.ArticleAttributeValues
            .Include(v => v.ArticleAttributeDefinition)
            .Include(v => v.SelectedOption)
            .Where(v => articleIds.Contains(v.ArticleId))
            .ToListAsync();

        return values.GroupBy(v => v.ArticleId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task SaveValuesAsync(int articleId, List<ArticleAttributeValue> newValues, string userName, string windowsUser)
    {
        var existingValues = await _context.ArticleAttributeValues
            .Where(v => v.ArticleId == articleId)
            .ToListAsync();

        foreach (var newVal in newValues)
        {
            var existing = existingValues.FirstOrDefault(e => e.ArticleAttributeDefinitionId == newVal.ArticleAttributeDefinitionId);

            // Skip if value is empty (no boolean set, no option selected)
            var hasValue = newVal.BooleanValue.HasValue || newVal.SelectedOptionId.HasValue;

            if (existing != null)
            {
                if (!hasValue)
                {
                    // Remove value if cleared
                    _context.ArticleAttributeValues.Remove(existing);
                }
                else
                {
                    existing.BooleanValue = newVal.BooleanValue;
                    existing.SelectedOptionId = newVal.SelectedOptionId;
                    existing.ModifiedAt = DateTime.Now;
                    existing.ModifiedBy = userName;
                    existing.ModifiedByWindows = windowsUser;
                }
            }
            else if (hasValue)
            {
                // Create new value
                newVal.ArticleId = articleId;
                newVal.CreatedAt = DateTime.Now;
                newVal.CreatedBy = userName;
                newVal.CreatedByWindows = windowsUser;
                _context.ArticleAttributeValues.Add(newVal);
            }
        }

        await _context.SaveChangesAsync();
    }

    // ========== Batch for BOM ==========

    public async Task<Dictionary<string, string?>> GetCategoryNamesByArticleNumbersAsync(List<string> articleNumbers)
    {
        if (!articleNumbers.Any())
            return new Dictionary<string, string?>();

        return await _context.Articles
            .Where(a => articleNumbers.Contains(a.ArticleNumber))
            .Select(a => new { a.ArticleNumber, CategoryName = a.ArticleCategory != null ? a.ArticleCategory.Name : null })
            .ToDictionaryAsync(a => a.ArticleNumber, a => a.CategoryName);
    }
}
```

### Edit: `IdealAkeWms/Program.cs` — Register repository

**After:**
```csharp
builder.Services.AddScoped<IArticleCategoryRepository, ArticleCategoryRepository>();
```

**Add:**
```csharp
builder.Services.AddScoped<IArticleAttributeRepository, ArticleAttributeRepository>();
```

### New file: `IdealAkeWms.Tests/Repositories/ArticleAttributeRepositoryTests.cs`

```csharp
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class ArticleAttributeRepositoryTests
{
    private static ArticleAttributeDefinition CreateDefinition(
        string name, AttributeType type = AttributeType.Boolean, int sortOrder = 0, bool isActive = true)
        => new()
        {
            Name = name,
            AttributeType = type,
            SortOrder = sortOrder,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        };

    private static Article CreateArticle(string number)
        => new()
        {
            ArticleNumber = number,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        };

    [Fact]
    public async Task GetAllDefinitionsAsync_ReturnsSortedWithOptions()
    {
        using var context = TestDbContextFactory.Create();
        var d1 = CreateDefinition("Material", AttributeType.Dropdown, 1);
        var d2 = CreateDefinition("Laserschnitt", AttributeType.Boolean, 0);
        context.ArticleAttributeDefinitions.AddRange(d1, d2);
        await context.SaveChangesAsync();

        context.ArticleAttributeOptions.AddRange(
            new ArticleAttributeOption { ArticleAttributeDefinitionId = d1.Id, Value = "Alu", SortOrder = 0 },
            new ArticleAttributeOption { ArticleAttributeDefinitionId = d1.Id, Value = "Stahl", SortOrder = 1 });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        var result = await repo.GetAllDefinitionsAsync();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Laserschnitt"); // SortOrder 0
        result[1].Name.Should().Be("Material");      // SortOrder 1
        result[1].Options.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveDefinitionsOrderedAsync_ExcludesInactive()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleAttributeDefinitions.AddRange(
            CreateDefinition("Active", isActive: true),
            CreateDefinition("Inactive", isActive: false));
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        var result = await repo.GetActiveDefinitionsOrderedAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active");
    }

    [Fact]
    public async Task DefinitionHasValuesAsync_ReturnsTrueWhenValuesExist()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Test");
        context.ArticleAttributeDefinitions.Add(def);
        var article = CreateArticle("ART1");
        context.Articles.Add(article);
        await context.SaveChangesAsync();

        context.ArticleAttributeValues.Add(new ArticleAttributeValue
        {
            ArticleId = article.Id,
            ArticleAttributeDefinitionId = def.Id,
            BooleanValue = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        (await repo.DefinitionHasValuesAsync(def.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task DefinitionHasValuesAsync_ReturnsFalseWhenNoValues()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Empty");
        context.ArticleAttributeDefinitions.Add(def);
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        (await repo.DefinitionHasValuesAsync(def.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task OptionIsInUseAsync_DetectsUsedOption()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Material", AttributeType.Dropdown);
        context.ArticleAttributeDefinitions.Add(def);
        await context.SaveChangesAsync();

        var option = new ArticleAttributeOption { ArticleAttributeDefinitionId = def.Id, Value = "Alu" };
        context.ArticleAttributeOptions.Add(option);
        var article = CreateArticle("ART1");
        context.Articles.Add(article);
        await context.SaveChangesAsync();

        context.ArticleAttributeValues.Add(new ArticleAttributeValue
        {
            ArticleId = article.Id,
            ArticleAttributeDefinitionId = def.Id,
            SelectedOptionId = option.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        (await repo.OptionIsInUseAsync(option.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task GetNextSortOrderAsync_ReturnsMaxPlusOne()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleAttributeDefinitions.AddRange(
            CreateDefinition("A", sortOrder: 0),
            CreateDefinition("B", sortOrder: 5));
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        var next = await repo.GetNextSortOrderAsync();
        next.Should().Be(6);
    }

    [Fact]
    public async Task GetNextSortOrderAsync_ReturnsZeroWhenEmpty()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new ArticleAttributeRepository(context);
        var next = await repo.GetNextSortOrderAsync();
        next.Should().Be(0);
    }

    [Fact]
    public async Task SaveValuesAsync_CreatesNewValues()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Test");
        context.ArticleAttributeDefinitions.Add(def);
        var article = CreateArticle("ART1");
        context.Articles.Add(article);
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        await repo.SaveValuesAsync(article.Id, new List<ArticleAttributeValue>
        {
            new() { ArticleAttributeDefinitionId = def.Id, BooleanValue = true }
        }, "TestUser", "DOMAIN\\test");

        var values = await repo.GetValuesByArticleIdAsync(article.Id);
        values.Should().HaveCount(1);
        values[0].BooleanValue.Should().BeTrue();
    }

    [Fact]
    public async Task SaveValuesAsync_UpdatesExistingValues()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Test");
        context.ArticleAttributeDefinitions.Add(def);
        var article = CreateArticle("ART1");
        context.Articles.Add(article);
        await context.SaveChangesAsync();

        context.ArticleAttributeValues.Add(new ArticleAttributeValue
        {
            ArticleId = article.Id,
            ArticleAttributeDefinitionId = def.Id,
            BooleanValue = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        await repo.SaveValuesAsync(article.Id, new List<ArticleAttributeValue>
        {
            new() { ArticleAttributeDefinitionId = def.Id, BooleanValue = true }
        }, "TestUser", "DOMAIN\\test");

        var values = await repo.GetValuesByArticleIdAsync(article.Id);
        values.Should().HaveCount(1);
        values[0].BooleanValue.Should().BeTrue();
    }

    [Fact]
    public async Task SaveValuesAsync_RemovesClearedValues()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Test");
        context.ArticleAttributeDefinitions.Add(def);
        var article = CreateArticle("ART1");
        context.Articles.Add(article);
        await context.SaveChangesAsync();

        context.ArticleAttributeValues.Add(new ArticleAttributeValue
        {
            ArticleId = article.Id,
            ArticleAttributeDefinitionId = def.Id,
            BooleanValue = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        // Send value without BooleanValue or SelectedOptionId — should remove
        await repo.SaveValuesAsync(article.Id, new List<ArticleAttributeValue>
        {
            new() { ArticleAttributeDefinitionId = def.Id }
        }, "TestUser", "DOMAIN\\test");

        var values = await repo.GetValuesByArticleIdAsync(article.Id);
        values.Should().BeEmpty();
    }

    [Fact]
    public async Task GetValuesByArticleIdsAsync_BatchLoadsCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Test");
        context.ArticleAttributeDefinitions.Add(def);
        var a1 = CreateArticle("ART1");
        var a2 = CreateArticle("ART2");
        context.Articles.AddRange(a1, a2);
        await context.SaveChangesAsync();

        context.ArticleAttributeValues.AddRange(
            new ArticleAttributeValue { ArticleId = a1.Id, ArticleAttributeDefinitionId = def.Id, BooleanValue = true, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" },
            new ArticleAttributeValue { ArticleId = a2.Id, ArticleAttributeDefinitionId = def.Id, BooleanValue = false, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        var result = await repo.GetValuesByArticleIdsAsync(new List<int> { a1.Id, a2.Id });

        result.Should().HaveCount(2);
        result[a1.Id].Should().HaveCount(1);
        result[a2.Id].Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCategoryNamesByArticleNumbersAsync_ReturnsCategoryNames()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new ArticleCategory { Name = "Bleche", CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" };
        context.ArticleCategories.Add(cat);
        await context.SaveChangesAsync();

        context.Articles.AddRange(
            new Article { ArticleNumber = "A1", ArticleCategoryId = cat.Id, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" },
            new Article { ArticleNumber = "A2", CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        var result = await repo.GetCategoryNamesByArticleNumbersAsync(new List<string> { "A1", "A2" });

        result.Should().HaveCount(2);
        result["A1"].Should().Be("Bleche");
        result["A2"].Should().BeNull();
    }
}
```

### Commands

```bash
cd /c/Entwicklung/C#/AKEBDELight
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests
```

### Commit

```
feat: add ArticleAttributeRepository with definitions, options, values, and tests
```

---

## Task 5: ArticleAttributesController + View

- [ ] Create `IdealAkeWms/Controllers/ArticleAttributesController.cs`
- [ ] Create `IdealAkeWms/Views/ArticleAttributes/Index.cshtml`
- [ ] Build + test
- [ ] Commit

### New file: `IdealAkeWms/Controllers/ArticleAttributesController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataAccess]
public class ArticleAttributesController : Controller
{
    private readonly IArticleAttributeRepository _attributeRepository;
    private readonly ICurrentUserService _currentUserService;

    public ArticleAttributesController(
        IArticleAttributeRepository attributeRepository,
        ICurrentUserService currentUserService)
    {
        _attributeRepository = attributeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index()
    {
        var definitions = await _attributeRepository.GetAllDefinitionsAsync();
        // Pre-load value counts for each definition
        var valueCounts = new Dictionary<int, bool>();
        foreach (var def in definitions)
        {
            valueCounts[def.Id] = await _attributeRepository.DefinitionHasValuesAsync(def.Id);
        }
        ViewBag.HasValues = valueCounts;
        return View(definitions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDefinition(string name, AttributeType attributeType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["WarningMessage"] = "Name darf nicht leer sein.";
            return RedirectToAction(nameof(Index));
        }

        if (await _attributeRepository.DefinitionExistsByNameAsync(name.Trim()))
        {
            TempData["WarningMessage"] = $"Merkmal '{name.Trim()}' existiert bereits.";
            return RedirectToAction(nameof(Index));
        }

        var definition = new ArticleAttributeDefinition
        {
            Name = name.Trim(),
            AttributeType = attributeType,
            SortOrder = await _attributeRepository.GetNextSortOrderAsync(),
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _attributeRepository.AddDefinitionAsync(definition);
        TempData["SuccessMessage"] = $"Merkmal '{name.Trim()}' erstellt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDefinition(int id, string name, int sortOrder, bool isActive)
    {
        var definition = await _attributeRepository.GetDefinitionByIdAsync(id);
        if (definition == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["WarningMessage"] = "Name darf nicht leer sein.";
            return RedirectToAction(nameof(Index));
        }

        definition.Name = name.Trim();
        definition.SortOrder = sortOrder;
        definition.IsActive = isActive;
        definition.ModifiedAt = DateTime.Now;
        definition.ModifiedBy = _currentUserService.GetDisplayName();
        definition.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _attributeRepository.UpdateDefinitionAsync(definition);
        TempData["SuccessMessage"] = $"Merkmal '{name.Trim()}' aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDefinition(int id)
    {
        var definition = await _attributeRepository.GetDefinitionByIdAsync(id);
        if (definition == null)
            return NotFound();

        if (await _attributeRepository.DefinitionHasValuesAsync(id))
        {
            TempData["WarningMessage"] = $"Merkmal '{definition.Name}' wird verwendet — bitte zuerst deaktivieren statt loeschen.";
            return RedirectToAction(nameof(Index));
        }

        var name = definition.Name;
        await _attributeRepository.DeleteDefinitionAsync(id);
        TempData["SuccessMessage"] = $"Merkmal '{name}' geloescht.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddOption(int definitionId, string value, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            TempData["WarningMessage"] = "Wert darf nicht leer sein.";
            return RedirectToAction(nameof(Index));
        }

        var definition = await _attributeRepository.GetDefinitionByIdAsync(definitionId);
        if (definition == null)
            return NotFound();

        var option = new ArticleAttributeOption
        {
            ArticleAttributeDefinitionId = definitionId,
            Value = value.Trim(),
            SortOrder = sortOrder
        };

        await _attributeRepository.AddOptionAsync(option);
        TempData["SuccessMessage"] = $"Option '{value.Trim()}' zu '{definition.Name}' hinzugefuegt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOption(int id)
    {
        if (await _attributeRepository.OptionIsInUseAsync(id))
        {
            TempData["WarningMessage"] = "Option wird von Artikeln verwendet und kann nicht geloescht werden.";
            return RedirectToAction(nameof(Index));
        }

        await _attributeRepository.DeleteOptionAsync(id);
        TempData["SuccessMessage"] = "Option geloescht.";
        return RedirectToAction(nameof(Index));
    }
}
```

### New file: `IdealAkeWms/Views/ArticleAttributes/Index.cshtml`

```html
@model List<IdealAkeWms.Models.ArticleAttributeDefinition>
@{
    ViewData["Title"] = "Artikelmerkmale";
    var hasValues = ViewBag.HasValues as Dictionary<int, bool> ?? new Dictionary<int, bool>();
}

<h2 class="page-header">Artikelmerkmale</h2>
<p class="text-muted mb-3">
    Definieren Sie Merkmale (Boolean/Dropdown) fuer Artikel. Merkmale erscheinen als filterbare Spalten in der Artikel-Uebersicht und als Eingabefelder in der Artikel-Bearbeitung.
</p>

<div class="row">
    <div class="col-lg-8 mb-4">
        <div class="card">
            <div class="card-header d-flex justify-content-between align-items-center flex-wrap gap-2">
                Definierte Merkmale (@Model.Count)
            </div>
            <div class="card-body p-0">
                <div class="table-responsive">
                    <table class="table table-striped mb-0">
                        <thead>
                            <tr>
                                <th>Name</th>
                                <th style="width: 100px;">Typ</th>
                                <th style="width: 100px;">Reihenfolge</th>
                                <th style="width: 80px;">Aktiv</th>
                                <th style="width: 140px;"></th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var def in Model)
                            {
                                hasValues.TryGetValue(def.Id, out var hasVals);
                                <tr>
                                    <td>
                                        <form asp-action="UpdateDefinition" method="post" class="d-flex align-items-center gap-1" id="form-def-@def.Id">
                                            @Html.AntiForgeryToken()
                                            <input type="hidden" name="id" value="@def.Id" />
                                            <input type="text" name="name" value="@def.Name" class="form-control form-control-sm" required />
                                        </form>
                                    </td>
                                    <td>
                                        @if (def.AttributeType == IdealAkeWms.Models.AttributeType.Boolean)
                                        {
                                            <span class="badge bg-primary">Boolean</span>
                                        }
                                        else
                                        {
                                            <span class="badge bg-success">Dropdown</span>
                                        }
                                    </td>
                                    <td>
                                        <input type="number" name="sortOrder" value="@def.SortOrder" form="form-def-@def.Id" class="form-control form-control-sm text-center" style="width: 80px;" />
                                    </td>
                                    <td class="text-center">
                                        <input type="hidden" name="isActive" value="@(def.IsActive ? "true" : "false")" form="form-def-@def.Id" id="hidden-active-@def.Id" />
                                        <input type="checkbox" class="form-check-input" form="form-def-@def.Id"
                                               @(def.IsActive ? "checked" : "")
                                               onchange="document.getElementById('hidden-active-@def.Id').value = this.checked ? 'true' : 'false'" />
                                    </td>
                                    <td>
                                        <div class="d-flex gap-1">
                                            <button type="submit" form="form-def-@def.Id" class="btn btn-sm btn-outline-primary" title="Speichern">
                                                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                                                    <path d="M12.736 3.97a.733.733 0 0 1 1.047 0c.286.289.29.756.01 1.05L7.88 12.01a.733.733 0 0 1-1.065.02L3.217 8.384a.757.757 0 0 1 0-1.06.733.733 0 0 1 1.047 0l3.052 3.093 5.4-6.425z"/>
                                                </svg>
                                            </button>
                                            @if (!hasVals)
                                            {
                                                <form asp-action="DeleteDefinition" method="post" class="d-inline">
                                                    @Html.AntiForgeryToken()
                                                    <input type="hidden" name="id" value="@def.Id" />
                                                    <button type="submit" class="btn btn-sm btn-outline-danger" title="Loeschen"
                                                            onclick="return confirm('Merkmal \'@def.Name\' wirklich loeschen?');">
                                                        <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                                                            <path d="M5.5 5.5A.5.5 0 0 1 6 6v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5m2.5 0a.5.5 0 0 1 .5.5v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5m3 .5a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0z"/>
                                                            <path d="M14.5 3a1 1 0 0 1-1 1H13v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4h-.5a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1H6a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1h3.5a1 1 0 0 1 1 1zM4.118 4 4 4.059V13a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V4.059L11.882 4zM2.5 3h11V2h-11z"/>
                                                        </svg>
                                                    </button>
                                                </form>
                                            }
                                            else
                                            {
                                                <span class="btn btn-sm btn-outline-secondary disabled" title="Merkmal wird verwendet — deaktivieren statt loeschen">
                                                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                                                        <path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14m0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16"/>
                                                        <path d="M4.646 4.646a.5.5 0 0 1 .708 0L8 7.293l2.646-2.647a.5.5 0 0 1 .708.708L8.707 8l2.647 2.646a.5.5 0 0 1-.708.708L8 8.707l-2.646 2.647a.5.5 0 0 1-.708-.708L7.293 8 4.646 5.354a.5.5 0 0 1 0-.708"/>
                                                    </svg>
                                                </span>
                                            }
                                        </div>
                                    </td>
                                </tr>
                                @* Dropdown options sub-section *@
                                @if (def.AttributeType == IdealAkeWms.Models.AttributeType.Dropdown)
                                {
                                    <tr class="table-light">
                                        <td colspan="5" style="padding-left: 2rem;">
                                            <details>
                                                <summary class="text-muted small fw-bold" style="cursor: pointer;">
                                                    Dropdown-Optionen (@def.Options.Count)
                                                </summary>
                                                <div class="mt-2">
                                                    @if (def.Options.Any())
                                                    {
                                                        <ul class="list-group list-group-flush mb-2">
                                                            @foreach (var opt in def.Options)
                                                            {
                                                                <li class="list-group-item d-flex justify-content-between align-items-center py-1 px-2">
                                                                    <span>@opt.Value <small class="text-muted">(Pos. @opt.SortOrder)</small></span>
                                                                    <form asp-action="DeleteOption" method="post" class="d-inline">
                                                                        @Html.AntiForgeryToken()
                                                                        <input type="hidden" name="id" value="@opt.Id" />
                                                                        <button type="submit" class="btn btn-sm btn-outline-danger py-0 px-1" title="Option loeschen"
                                                                                onclick="return confirm('Option \'@opt.Value\' wirklich loeschen?');">
                                                                            &times;
                                                                        </button>
                                                                    </form>
                                                                </li>
                                                            }
                                                        </ul>
                                                    }
                                                    <form asp-action="AddOption" method="post" class="d-flex gap-2 align-items-end">
                                                        @Html.AntiForgeryToken()
                                                        <input type="hidden" name="definitionId" value="@def.Id" />
                                                        <input type="text" name="value" class="form-control form-control-sm" placeholder="Neuer Wert" required style="max-width: 200px;" />
                                                        <input type="number" name="sortOrder" value="@(def.Options.Any() ? def.Options.Max(o => o.SortOrder) + 1 : 0)" class="form-control form-control-sm text-center" style="width: 80px;" title="Reihenfolge" />
                                                        <button type="submit" class="btn btn-sm btn-outline-primary">Hinzufuegen</button>
                                                    </form>
                                                </div>
                                            </details>
                                        </td>
                                    </tr>
                                }
                            }
                            @if (!Model.Any())
                            {
                                <tr>
                                    <td colspan="5" class="text-center text-muted py-3">Keine Merkmale definiert.</td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>

    <div class="col-lg-4 mb-4">
        <div class="card">
            <div class="card-header">Neues Merkmal erstellen</div>
            <div class="card-body">
                <form asp-action="CreateDefinition" method="post">
                    @Html.AntiForgeryToken()
                    <div class="mb-3">
                        <label class="form-label fw-bold">Name</label>
                        <input type="text" name="name" class="form-control" placeholder="z.B. Laserschnitt" required />
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold">Typ</label>
                        <select name="attributeType" class="form-select">
                            <option value="0">Boolean (Ja/Nein)</option>
                            <option value="1">Dropdown (Auswahlliste)</option>
                        </select>
                        <small class="text-muted">Typ kann nach Erstellung nicht geaendert werden.</small>
                    </div>
                    <button type="submit" class="btn btn-primary w-100">Erstellen</button>
                </form>
            </div>
        </div>

        <div class="card mt-3">
            <div class="card-header">Hinweise</div>
            <div class="card-body small">
                <p class="mb-2"><strong>Boolean</strong> = Checkbox (Ja/Nein) in der Artikelbearbeitung.</p>
                <p class="mb-2"><strong>Dropdown</strong> = Auswahlliste mit vordefinierten Optionen. Optionen koennen nach Erstellung hinzugefuegt werden.</p>
                <p class="mb-2"><strong>Reihenfolge</strong> bestimmt die Spalten-Position in der Artikel-Uebersicht.</p>
                <p class="mb-0"><strong>Deaktivierte</strong> Merkmale verschwinden aus der Artikel-Uebersicht/Bearbeitung, Werte bleiben in der DB erhalten.</p>
            </div>
        </div>
    </div>
</div>
```

### Commands

```bash
cd /c/Entwicklung/C#/AKEBDELight
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests
```

### Commit

```
feat: add ArticleAttributesController with CRUD view for definitions and dropdown options
```

---

## Task 6: Article Index — Category + Attribute Columns

- [ ] Extend `IdealAkeWms/Models/ViewModels/ArticleIndexViewModel.cs` with category and attribute data
- [ ] Extend `IdealAkeWms/Data/Repositories/ArticleRepository.cs` `GetPaginatedAsync` with `.Include(a => a.ArticleCategory)`
- [ ] Extend `IdealAkeWms/Controllers/ArticlesController.cs` Index action to load attributes
- [ ] Update `IdealAkeWms/Views/Articles/Index.cshtml` with dynamic columns
- [ ] Build + test
- [ ] Commit

### Edit: `IdealAkeWms/Models/ViewModels/ArticleIndexViewModel.cs`

**Old:**
```csharp
namespace IdealAkeWms.Models.ViewModels;

public class ArticleIndexViewModel
{
    public List<Article> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string? Search { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

**New:**
```csharp
namespace IdealAkeWms.Models.ViewModels;

public class ArticleIndexViewModel
{
    public List<Article> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string? Search { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Active attribute definitions (for dynamic column headers).</summary>
    public List<ArticleAttributeDefinition> AttributeDefinitions { get; set; } = new();

    /// <summary>Attribute values keyed by ArticleId, then by DefinitionId.</summary>
    public Dictionary<int, List<ArticleAttributeValue>> AttributeValuesByArticle { get; set; } = new();
}
```

### Edit: `IdealAkeWms/Data/Repositories/ArticleRepository.cs` — GetPaginatedAsync

**Old:**
```csharp
    public async Task<(List<Article> Items, int TotalCount)> GetPaginatedAsync(int page, int pageSize, string? search)
    {
        var query = _dbSet.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.ArticleNumber.Contains(search) ||
                                     (a.Description != null && a.Description.Contains(search)));
        }
        var totalCount = await query.CountAsync();
        var items = await query.OrderBy(a => a.ArticleNumber)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();
        return (items, totalCount);
    }
```

**New:**
```csharp
    public async Task<(List<Article> Items, int TotalCount)> GetPaginatedAsync(int page, int pageSize, string? search)
    {
        var query = _dbSet.Include(a => a.ArticleCategory).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.ArticleNumber.Contains(search) ||
                                     (a.Description != null && a.Description.Contains(search)));
        }
        var totalCount = await query.CountAsync();
        var items = await query.OrderBy(a => a.ArticleNumber)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();
        return (items, totalCount);
    }
```

### Edit: `IdealAkeWms/Controllers/ArticlesController.cs` — Index action

**Old:**
```csharp
    public async Task<IActionResult> Index(int page = 1, int pageSize = 100, string? search = null)
    {
        if (pageSize > 500) pageSize = 500;
        var (items, totalCount) = await _articleRepository.GetPaginatedAsync(page, pageSize, search);
        var vm = new ArticleIndexViewModel
        {
            Items = items,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = search
        };
        return View(vm);
    }
```

**New:**
```csharp
    public async Task<IActionResult> Index(int page = 1, int pageSize = 100, string? search = null)
    {
        if (pageSize > 500) pageSize = 500;
        var (items, totalCount) = await _articleRepository.GetPaginatedAsync(page, pageSize, search);

        // Load active attribute definitions + batch-load values for displayed articles
        var activeDefinitions = await _attributeRepository.GetActiveDefinitionsOrderedAsync();
        var articleIds = items.Select(a => a.Id).ToList();
        var attributeValues = await _attributeRepository.GetValuesByArticleIdsAsync(articleIds);

        var vm = new ArticleIndexViewModel
        {
            Items = items,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = search,
            AttributeDefinitions = activeDefinitions,
            AttributeValuesByArticle = attributeValues
        };
        return View(vm);
    }
```

### Edit: `IdealAkeWms/Controllers/ArticlesController.cs` — Add dependency injection

**Old:**
```csharp
public class ArticlesController : Controller
{
    private readonly IArticleRepository _articleRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUserService;

    public ArticlesController(
        IArticleRepository articleRepository,
        IStockMovementRepository stockMovementRepository,
        ICurrentUserService currentUserService)
    {
        _articleRepository = articleRepository;
        _stockMovementRepository = stockMovementRepository;
        _currentUserService = currentUserService;
    }
```

**New:**
```csharp
public class ArticlesController : Controller
{
    private readonly IArticleRepository _articleRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IArticleAttributeRepository _attributeRepository;
    private readonly IArticleCategoryRepository _categoryRepository;

    public ArticlesController(
        IArticleRepository articleRepository,
        IStockMovementRepository stockMovementRepository,
        ICurrentUserService currentUserService,
        IArticleAttributeRepository attributeRepository,
        IArticleCategoryRepository categoryRepository)
    {
        _articleRepository = articleRepository;
        _stockMovementRepository = stockMovementRepository;
        _currentUserService = currentUserService;
        _attributeRepository = attributeRepository;
        _categoryRepository = categoryRepository;
    }
```

Also add the using statement at the top of the file if not already present:

```csharp
using IdealAkeWms.Data.Repositories;
```

### Edit: `IdealAkeWms/Views/Articles/Index.cshtml`

**Old:**
```html
<div class="table-responsive">
    <table class="table table-striped mb-0">
        <thead>
            <tr>
                <th>Artikelnummer</th>
                <th>Bezeichnung</th>
                <th>Einheit</th>
                <th>Artikelgruppe</th>
                <th>Erstellt am</th>
                <th style="width: 100px;"></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var article in Model.Items)
            {
                <tr>
                    <td>
                        <strong>@article.ArticleNumber</strong>
                        <a href="http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search=@article.ArticleNumber&searchContext=0" target="_blank" title="Zeichnung in Vault öffnen" class="vault-link">
                            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                                <path d="M14 4.5V14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h5.5zm-3 0A1.5 1.5 0 0 1 9.5 3V1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V4.5z"/>
                                <path d="M4.5 12.5A.5.5 0 0 1 5 12h3a.5.5 0 0 1 0 1H5a.5.5 0 0 1-.5-.5m0-2A.5.5 0 0 1 5 10h6a.5.5 0 0 1 0 1H5a.5.5 0 0 1-.5-.5m1.639-3.708 1.33.886 1.854-1.855a.25.25 0 0 1 .289-.047l1.888.974V7.5a.5.5 0 0 1-.5.5H5a.5.5 0 0 1-.5-.5V7s1.54-1.274 1.639-1.208M6.25 6a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5"/>
                            </svg>
                        </a>
                    </td>
                    <td>@article.Description</td>
                    <td>@article.Unit</td>
                    <td>@article.ArticleGroup</td>
                    <td>@article.CreatedAt.ToString("dd.MM.yyyy HH:mm")</td>
                    <td>
                        <a asp-action="Edit" asp-route-id="@article.Id" class="btn btn-sm btn-secondary">Bearbeiten</a>
                    </td>
                </tr>
            }
            @if (!Model.Items.Any())
            {
                <tr>
                    <td colspan="6" class="text-center text-muted py-4">Keine Artikel vorhanden.</td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

**New:**
```html
@{
    var attrDefs = Model.AttributeDefinitions;
    var attrValues = Model.AttributeValuesByArticle;
    var totalColCount = 6 + attrDefs.Count + 1; // base cols + category + dynamic attrs
}

<div class="table-responsive">
    <table class="table table-striped mb-0 filterable-table">
        <thead>
            <tr>
                @{ var colIdx = 0; }
                <th data-filterable data-col="@colIdx">Artikelnummer</th>
                @{ colIdx++; }
                <th data-filterable data-col="@colIdx">Bezeichnung</th>
                @{ colIdx++; }
                <th data-filterable data-col="@colIdx">Einheit</th>
                @{ colIdx++; }
                <th data-filterable data-col="@colIdx">Artikelgruppe</th>
                @{ colIdx++; }
                <th data-filterable data-col="@colIdx">Kategorie</th>
                @{ colIdx++; }
                @foreach (var def in attrDefs)
                {
                    <th data-filterable data-col="@colIdx">@def.Name</th>
                    colIdx++;
                }
                <th>Erstellt am</th>
                <th style="width: 100px;"></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var article in Model.Items)
            {
                attrValues.TryGetValue(article.Id, out var articleAttrs);
                <tr>
                    <td>
                        <strong>@article.ArticleNumber</strong>
                        <a href="http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search=@article.ArticleNumber&searchContext=0" target="_blank" title="Zeichnung in Vault oeffnen" class="vault-link">
                            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                                <path d="M14 4.5V14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h5.5zm-3 0A1.5 1.5 0 0 1 9.5 3V1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V4.5z"/>
                                <path d="M4.5 12.5A.5.5 0 0 1 5 12h3a.5.5 0 0 1 0 1H5a.5.5 0 0 1-.5-.5m0-2A.5.5 0 0 1 5 10h6a.5.5 0 0 1 0 1H5a.5.5 0 0 1-.5-.5m1.639-3.708 1.33.886 1.854-1.855a.25.25 0 0 1 .289-.047l1.888.974V7.5a.5.5 0 0 1-.5.5H5a.5.5 0 0 1-.5-.5V7s1.54-1.274 1.639-1.208M6.25 6a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5"/>
                            </svg>
                        </a>
                    </td>
                    <td>@article.Description</td>
                    <td>@article.Unit</td>
                    <td>@article.ArticleGroup</td>
                    <td>@(article.ArticleCategory?.Name ?? "")</td>
                    @foreach (var def in attrDefs)
                    {
                        var val = articleAttrs?.FirstOrDefault(v => v.ArticleAttributeDefinitionId == def.Id);
                        <td>
                            @if (val != null)
                            {
                                if (def.AttributeType == IdealAkeWms.Models.AttributeType.Boolean)
                                {
                                    @(val.BooleanValue == true ? "Ja" : "Nein")
                                }
                                else
                                {
                                    @(val.SelectedOption?.Value ?? "")
                                }
                            }
                        </td>
                    }
                    <td>@article.CreatedAt.ToString("dd.MM.yyyy HH:mm")</td>
                    <td>
                        <a asp-action="Edit" asp-route-id="@article.Id" class="btn btn-sm btn-secondary">Bearbeiten</a>
                    </td>
                </tr>
            }
            @if (!Model.Items.Any())
            {
                <tr>
                    <td colspan="@totalColCount" class="text-center text-muted py-4">Keine Artikel vorhanden.</td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

### Commands

```bash
cd /c/Entwicklung/C#/AKEBDELight
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests
```

### Commit

```
feat: extend Article Index with category column and dynamic attribute columns
```

---

## Task 7: Article Edit — Category Dropdown + Attribute Fields

- [ ] Create `IdealAkeWms/Models/ViewModels/ArticleEditViewModel.cs`
- [ ] Extend `IdealAkeWms/Controllers/ArticlesController.cs` Edit GET/POST
- [ ] Update `IdealAkeWms/Views/Articles/Edit.cshtml`
- [ ] Update `IdealAkeWms/Views/Articles/Info.cshtml` with read-only display
- [ ] Build + test
- [ ] Commit

### New file: `IdealAkeWms/Models/ViewModels/ArticleEditViewModel.cs`

```csharp
namespace IdealAkeWms.Models.ViewModels;

public class ArticleEditViewModel
{
    public Article Article { get; set; } = null!;
    public List<ArticleCategory> Categories { get; set; } = new();
    public List<AttributeEditItem> Attributes { get; set; } = new();
}

public class AttributeEditItem
{
    public int DefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AttributeType AttributeType { get; set; }
    public bool? BooleanValue { get; set; }
    public int? SelectedOptionId { get; set; }
    public List<ArticleAttributeOption> Options { get; set; } = new();
}
```

### Edit: `IdealAkeWms/Controllers/ArticlesController.cs` — Edit GET

**Old:**
```csharp
    public async Task<IActionResult> Edit(int id)
    {
        var article = await _articleRepository.GetByIdAsync(id);
        if (article == null)
            return NotFound();

        return View(article);
    }
```

**New:**
```csharp
    public async Task<IActionResult> Edit(int id)
    {
        var article = await _articleRepository.GetByIdAsync(id);
        if (article == null)
            return NotFound();

        var categories = await _categoryRepository.GetAllOrderedAsync();
        var activeDefinitions = await _attributeRepository.GetActiveDefinitionsOrderedAsync();
        var existingValues = await _attributeRepository.GetValuesByArticleIdAsync(id);

        var attributeItems = activeDefinitions.Select(def =>
        {
            var existing = existingValues.FirstOrDefault(v => v.ArticleAttributeDefinitionId == def.Id);
            return new AttributeEditItem
            {
                DefinitionId = def.Id,
                Name = def.Name,
                AttributeType = def.AttributeType,
                BooleanValue = existing?.BooleanValue,
                SelectedOptionId = existing?.SelectedOptionId,
                Options = def.Options.ToList()
            };
        }).ToList();

        var vm = new ArticleEditViewModel
        {
            Article = article,
            Categories = categories,
            Attributes = attributeItems
        };

        return View(vm);
    }
```

### Edit: `IdealAkeWms/Controllers/ArticlesController.cs` — Edit POST

**Old:**
```csharp
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Article article)
    {
        if (id != article.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(article);

        var existing = await _articleRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.ArticleNumber = article.ArticleNumber;
        existing.Description = article.Description;
        existing.Unit = article.Unit;
        existing.ReorderLevel = article.ReorderLevel;
        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _articleRepository.UpdateAsync(existing);
        return RedirectToAction(nameof(Index));
    }
```

**New:**
```csharp
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ArticleEditViewModel vm)
    {
        if (id != vm.Article.Id)
            return NotFound();

        // Re-populate for validation failure
        if (!ModelState.IsValid)
        {
            vm.Categories = await _categoryRepository.GetAllOrderedAsync();
            var activeDefinitions = await _attributeRepository.GetActiveDefinitionsOrderedAsync();
            // Re-fill options for dropdown attributes
            foreach (var attr in vm.Attributes)
            {
                var def = activeDefinitions.FirstOrDefault(d => d.Id == attr.DefinitionId);
                if (def != null)
                {
                    attr.Name = def.Name;
                    attr.AttributeType = def.AttributeType;
                    attr.Options = def.Options.ToList();
                }
            }
            return View(vm);
        }

        var existing = await _articleRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.ArticleNumber = vm.Article.ArticleNumber;
        existing.Description = vm.Article.Description;
        existing.Unit = vm.Article.Unit;
        existing.ReorderLevel = vm.Article.ReorderLevel;
        existing.ArticleCategoryId = vm.Article.ArticleCategoryId;
        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _articleRepository.UpdateAsync(existing);

        // Save attribute values
        var attributeValues = vm.Attributes.Select(a => new ArticleAttributeValue
        {
            ArticleAttributeDefinitionId = a.DefinitionId,
            BooleanValue = a.BooleanValue,
            SelectedOptionId = a.SelectedOptionId
        }).ToList();

        await _attributeRepository.SaveValuesAsync(
            id,
            attributeValues,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        TempData["SuccessMessage"] = "Artikel gespeichert.";
        return RedirectToAction(nameof(Index));
    }
```

### Edit: `IdealAkeWms/Views/Articles/Edit.cshtml` — Full replacement

**Old (entire file):**
```html
@model Article
@{
    ViewData["Title"] = "Artikel bearbeiten";
}

<h2 class="page-header">Artikel bearbeiten</h2>

<div class="row">
    <div class="col-md-6">
        <div class="card">
            <div class="card-body">
                <form asp-action="Edit" method="post">
                    <input type="hidden" asp-for="Id" />
                    <input type="hidden" asp-for="CreatedAt" />
                    <input type="hidden" asp-for="CreatedBy" />
                    <input type="hidden" asp-for="CreatedByWindows" />
                    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

                    <div class="mb-3">
                        <label asp-for="ArticleNumber" class="form-label"></label>
                        <input asp-for="ArticleNumber" class="form-control" autofocus />
                        <span asp-validation-for="ArticleNumber" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="Description" class="form-label"></label>
                        <input asp-for="Description" class="form-control" />
                        <span asp-validation-for="Description" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="Unit" class="form-label"></label>
                        <input asp-for="Unit" class="form-control" placeholder="z.B. Stk, kg, m" />
                        <span asp-validation-for="Unit" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="ArticleGroup" class="form-label"></label>
                        <input asp-for="ArticleGroup" class="form-control" readonly />
                    </div>

                    <div class="mb-3">
                        <label asp-for="ReorderLevel" class="form-label"></label>
                        <input asp-for="ReorderLevel" class="form-control" type="number" step="0.001" min="0" />
                        <small class="text-muted">Mindestbestand für Warnung in der Bestandsliste</small>
                        <span asp-validation-for="ReorderLevel" class="text-danger"></span>
                    </div>

                    <div class="d-flex gap-2">
                        <button type="submit" class="btn btn-primary">Speichern</button>
                        <a asp-action="Index" class="btn btn-outline-secondary">Abbrechen</a>
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

**New:**
```html
@model IdealAkeWms.Models.ViewModels.ArticleEditViewModel
@{
    ViewData["Title"] = "Artikel bearbeiten";
}

<h2 class="page-header">Artikel bearbeiten</h2>

<div class="row">
    <div class="col-md-6">
        <div class="card mb-3">
            <div class="card-header">Stammdaten</div>
            <div class="card-body">
                <form asp-action="Edit" method="post">
                    <input type="hidden" asp-for="Article.Id" />
                    <input type="hidden" asp-for="Article.CreatedAt" />
                    <input type="hidden" asp-for="Article.CreatedBy" />
                    <input type="hidden" asp-for="Article.CreatedByWindows" />
                    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

                    <div class="mb-3">
                        <label asp-for="Article.ArticleNumber" class="form-label"></label>
                        <input asp-for="Article.ArticleNumber" class="form-control" autofocus />
                        <span asp-validation-for="Article.ArticleNumber" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="Article.Description" class="form-label"></label>
                        <input asp-for="Article.Description" class="form-control" />
                        <span asp-validation-for="Article.Description" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="Article.Unit" class="form-label"></label>
                        <input asp-for="Article.Unit" class="form-control" placeholder="z.B. Stk, kg, m" />
                        <span asp-validation-for="Article.Unit" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="Article.ArticleGroup" class="form-label"></label>
                        <input asp-for="Article.ArticleGroup" class="form-control" readonly />
                    </div>

                    <div class="mb-3">
                        <label asp-for="Article.ArticleCategoryId" class="form-label">Kategorie</label>
                        <select asp-for="Article.ArticleCategoryId" class="form-select">
                            <option value="">— Keine Kategorie —</option>
                            @foreach (var cat in Model.Categories)
                            {
                                <option value="@cat.Id">@cat.Name</option>
                            }
                        </select>
                    </div>

                    <div class="mb-3">
                        <label asp-for="Article.ReorderLevel" class="form-label"></label>
                        <input asp-for="Article.ReorderLevel" class="form-control" type="number" step="0.001" min="0" />
                        <small class="text-muted">Mindestbestand fuer Warnung in der Bestandsliste</small>
                        <span asp-validation-for="Article.ReorderLevel" class="text-danger"></span>
                    </div>

                    @if (Model.Attributes.Any())
                    {
                        <hr />
                        <h6 class="text-muted mb-3">Merkmale</h6>

                        @for (var i = 0; i < Model.Attributes.Count; i++)
                        {
                            var attr = Model.Attributes[i];
                            <input type="hidden" name="Attributes[@i].DefinitionId" value="@attr.DefinitionId" />

                            <div class="mb-3">
                                <label class="form-label">@attr.Name</label>
                                @if (attr.AttributeType == IdealAkeWms.Models.AttributeType.Boolean)
                                {
                                    <div class="form-check form-switch">
                                        <input type="hidden" name="Attributes[@i].BooleanValue" value="@(attr.BooleanValue == true ? "true" : "false")" id="hidden-bool-@i" />
                                        <input type="checkbox" class="form-check-input" id="attr-bool-@i"
                                               @(attr.BooleanValue == true ? "checked" : "")
                                               onchange="document.getElementById('hidden-bool-@i').value = this.checked ? 'true' : 'false'" />
                                        <label class="form-check-label" for="attr-bool-@i">Ja</label>
                                    </div>
                                }
                                else
                                {
                                    <select name="Attributes[@i].SelectedOptionId" class="form-select">
                                        <option value="">— Nicht gesetzt —</option>
                                        @foreach (var opt in attr.Options)
                                        {
                                            <option value="@opt.Id" selected="@(attr.SelectedOptionId == opt.Id)">@opt.Value</option>
                                        }
                                    </select>
                                }
                            </div>
                        }
                    }

                    <div class="d-flex gap-2">
                        <button type="submit" class="btn btn-primary">Speichern</button>
                        <a asp-action="Index" class="btn btn-outline-secondary">Abbrechen</a>
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

### Edit: `IdealAkeWms/Views/Articles/Info.cshtml` — Add category + attributes after Artikelgruppe

**Old (lines 71-72, after Artikelgruppe dd):**
```html
                        <dt class="col-sm-4">Artikelgruppe</dt>
                        <dd class="col-sm-8">@(Model.ArticleGroup ?? "—")</dd>
```

**New:**
```html
                        <dt class="col-sm-4">Artikelgruppe</dt>
                        <dd class="col-sm-8">@(Model.ArticleGroup ?? "—")</dd>

                        <dt class="col-sm-4">Kategorie</dt>
                        <dd class="col-sm-8">@(Model.CategoryName ?? "—")</dd>

                        @if (Model.AttributeDisplayValues.Any())
                        {
                            @foreach (var attr in Model.AttributeDisplayValues)
                            {
                                <dt class="col-sm-4">@attr.Name</dt>
                                <dd class="col-sm-8">@attr.DisplayValue</dd>
                            }
                        }
```

### Edit: `IdealAkeWms/Models/ViewModels/ArticleInfoViewModel.cs`

**Old:**
```csharp
namespace IdealAkeWms.Models.ViewModels;

public class ArticleInfoViewModel
{
    public string ArticleNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? ArticleGroup { get; set; }
    public decimal ReorderLevel { get; set; }
    public string VaultUrl { get; set; } = string.Empty;
    public List<StockOverviewItem> StockByLocation { get; set; } = new();
    public decimal TotalStock => StockByLocation.Sum(s => s.CurrentQuantity);
}
```

**New:**
```csharp
namespace IdealAkeWms.Models.ViewModels;

public class ArticleInfoViewModel
{
    public string ArticleNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? ArticleGroup { get; set; }
    public decimal ReorderLevel { get; set; }
    public string VaultUrl { get; set; } = string.Empty;
    public List<StockOverviewItem> StockByLocation { get; set; } = new();
    public decimal TotalStock => StockByLocation.Sum(s => s.CurrentQuantity);

    public string? CategoryName { get; set; }
    public List<AttributeDisplayValue> AttributeDisplayValues { get; set; } = new();
}

public class AttributeDisplayValue
{
    public string Name { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
}
```

### Edit: `IdealAkeWms/Controllers/ArticlesController.cs` — Info action

**Old:**
```csharp
        var vm = new ArticleInfoViewModel
        {
            ArticleNumber = article.ArticleNumber,
            Description = article.Description ?? string.Empty,
            Unit = article.Unit,
            ArticleGroup = article.ArticleGroup,
            ReorderLevel = article.ReorderLevel ?? 0,
            VaultUrl = $"http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search={Uri.EscapeDataString(article.ArticleNumber)}&searchContext=0",
            StockByLocation = stock
        };
        return View(vm);
```

**New:**
```csharp
        // Load category
        string? categoryName = null;
        if (article.ArticleCategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(article.ArticleCategoryId.Value);
            categoryName = category?.Name;
        }

        // Load attribute values
        var activeDefinitions = await _attributeRepository.GetActiveDefinitionsOrderedAsync();
        var attrValues = await _attributeRepository.GetValuesByArticleIdAsync(article.Id);
        var attrDisplayValues = activeDefinitions.Select(def =>
        {
            var val = attrValues.FirstOrDefault(v => v.ArticleAttributeDefinitionId == def.Id);
            string displayValue = "—";
            if (val != null)
            {
                if (def.AttributeType == AttributeType.Boolean)
                    displayValue = val.BooleanValue == true ? "Ja" : "Nein";
                else
                    displayValue = val.SelectedOption?.Value ?? "—";
            }
            return new AttributeDisplayValue { Name = def.Name, DisplayValue = displayValue };
        }).ToList();

        var vm = new ArticleInfoViewModel
        {
            ArticleNumber = article.ArticleNumber,
            Description = article.Description ?? string.Empty,
            Unit = article.Unit,
            ArticleGroup = article.ArticleGroup,
            ReorderLevel = article.ReorderLevel ?? 0,
            VaultUrl = $"http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search={Uri.EscapeDataString(article.ArticleNumber)}&searchContext=0",
            StockByLocation = stock,
            CategoryName = categoryName,
            AttributeDisplayValues = attrDisplayValues
        };
        return View(vm);
```

Add necessary using at top of controller file:

```csharp
using IdealAkeWms.Models;
```

### Commands

```bash
cd /c/Entwicklung/C#/AKEBDELight
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests
```

### Commit

```
feat: extend Article Edit with category dropdown and dynamic attribute fields, update Info view
```

---

## Task 8: BOM — Category Column

- [ ] Add `KategorieName` to `BomItemViewModel` in `IdealAkeWms/Models/ViewModels/BomViewModels.cs`
- [ ] Extend `IdealAkeWms/Controllers/PickingController.cs` Bom action to load categories per batch
- [ ] Update `IdealAkeWms/Views/Picking/Bom.cshtml` with new column
- [ ] Build + test
- [ ] Commit

### Edit: `IdealAkeWms/Models/ViewModels/BomViewModels.cs` — BomItemViewModel

**Old:**
```csharp
public class BomItemViewModel
{
    public string Artikelnummer { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Baugruppe { get; set; }
    public string? Ressourcenummer { get; set; }
    public string? Bezeichnung1 { get; set; }
    public string? Bezeichnung2 { get; set; }
    public decimal Menge { get; set; }
    public string? Beschaffungsartikel { get; set; }
    public string? Artikelgruppe { get; set; }
    public List<StockLocationInfo> StockLocations { get; set; } = new();
```

**New:**
```csharp
public class BomItemViewModel
{
    public string Artikelnummer { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Baugruppe { get; set; }
    public string? Ressourcenummer { get; set; }
    public string? Bezeichnung1 { get; set; }
    public string? Bezeichnung2 { get; set; }
    public decimal Menge { get; set; }
    public string? Beschaffungsartikel { get; set; }
    public string? Artikelgruppe { get; set; }
    public string? KategorieName { get; set; }
    public List<StockLocationInfo> StockLocations { get; set; } = new();
```

### Edit: `IdealAkeWms/Controllers/PickingController.cs` — Bom action

Inject `IArticleAttributeRepository` into `PickingController` constructor. Add the field:

```csharp
private readonly IArticleAttributeRepository _articleAttributeRepository;
```

And add to constructor parameter + assignment. Then in the Bom action, **after** the line:

```csharp
var stockByArticle = await _stockMovementRepository.GetStockByArticleNumbersAsync(articleNumbers);
```

**Add:**
```csharp
// Batch-load category names for BOM articles
var categoryByArticle = await _articleAttributeRepository.GetCategoryNamesByArticleNumbersAsync(articleNumbers);
```

Then in the `bomItems.Select(bom =>` lambda, where `BomItemViewModel` is constructed, **after** `Artikelgruppe = bom.Artikelgruppe,`:

**Old:**
```csharp
                Artikelgruppe = bom.Artikelgruppe,
                StockLocations = locations,
```

**New:**
```csharp
                Artikelgruppe = bom.Artikelgruppe,
                KategorieName = categoryByArticle.TryGetValue(bom.Ressourcenummer ?? "", out var catName) ? catName : null,
                StockLocations = locations,
```

### Edit: `IdealAkeWms/Views/Picking/Bom.cshtml` — Add Kategorie column header

**Old (in the thead):**
```html
                <th data-filterable data-col="8">Artikelgruppe</th>
                <th>Lagerplatz</th>
```

**New:**
```html
                <th data-filterable data-col="8">Artikelgruppe</th>
                <th data-filterable data-col="9">Kategorie</th>
                <th>Lagerplatz</th>
```

**Note:** All subsequent `data-col` indices in the BOM table need to be shifted by +1 for columns after Kategorie. However, since Lagerplatz and the remaining columns don't have `data-col` attributes, only the Bestellen column (if present) would be affected. Check and adjust if needed.

And in the tbody row, **after** `<td>@item.Artikelgruppe</td>`:

**Old:**
```html
                    <td>@item.Artikelgruppe</td>
                    <td>
                        @if (item.StockLocations.Any())
```

**New:**
```html
                    <td>@item.Artikelgruppe</td>
                    <td>@(item.KategorieName ?? "")</td>
                    <td>
                        @if (item.StockLocations.Any())
```

### Commands

```bash
cd /c/Entwicklung/C#/AKEBDELight
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests
```

### Commit

```
feat: add Kategorie column to BOM view with batch-loaded category names
```

---

## Task 9: OSEON Category Sync

- [ ] Add `SyncArticleCategoriesToWmsAsync` to `IDEALAKEWMSService/Services/IOseonSyncService.cs`
- [ ] Implement in `IDEALAKEWMSService/Services/OseonSyncService.cs`
- [ ] Add toggle `Sync:OseonArticleCategoryEnabled` to `IDEALAKEWMSService/appsettings.json`
- [ ] Integrate into `IDEALAKEWMSService/Workers/SyncWorker.cs`
- [ ] Build + test
- [ ] Commit

### Edit: `IDEALAKEWMSService/Services/IOseonSyncService.cs`

**Old:**
```csharp
public interface IOseonSyncService
{
    Task<SyncResult> SyncOseonProductionOrdersAsync(bool dryRun, CancellationToken ct = default);
    Task<SyncResult> SyncWorkplacesToProductionOrdersAsync(bool dryRun, CancellationToken ct = default);
}
```

**New:**
```csharp
public interface IOseonSyncService
{
    Task<SyncResult> SyncOseonProductionOrdersAsync(bool dryRun, CancellationToken ct = default);
    Task<SyncResult> SyncWorkplacesToProductionOrdersAsync(bool dryRun, CancellationToken ct = default);
    Task<SyncResult> SyncArticleCategoriesToWmsAsync(bool dryRun, CancellationToken ct = default);
}
```

### Edit: `IDEALAKEWMSService/Services/OseonSyncService.cs` — Add new method at end of class

Add the following method inside the `OseonSyncService` class, before the closing `}`:

```csharp
    public async Task<SyncResult> SyncArticleCategoriesToWmsAsync(bool dryRun, CancellationToken ct = default)
    {
        var oseonConnection = _configuration.GetConnectionString("OseonConnection")
            ?? throw new InvalidOperationException("OseonConnection nicht konfiguriert.");
        var wmsConnection = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");

        if (dryRun)
            _logger.LogInformation("[DryRun] OSEON-Artikelkategorie-Sync — keine Aenderungen werden geschrieben.");

        int inserted = 0, updated = 0, errors = 0;
        string? errorDetails = null;

        try
        {
            // Step 1: Read categories from OSEON
            var oseonCategories = new List<(string Name, string? Bemerkung, int? Typ)>();
            await using (var oseonConn = new SqlConnection(oseonConnection))
            {
                await oseonConn.OpenAsync(ct);
                await using var cmd = new SqlCommand("SELECT Name, Bemerkung, Typ FROM ArtikelKategorie", oseonConn)
                    { CommandTimeout = 60 };
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var name = reader.GetString(0).Trim();
                    var bemerkung = reader.IsDBNull(1) ? null : reader.GetString(1).Trim();
                    var typ = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                    if (!string.IsNullOrEmpty(name))
                        oseonCategories.Add((name, bemerkung, typ));
                }
            }

            _logger.LogInformation("OSEON-Artikelkategorien: {Count} Kategorien gelesen.", oseonCategories.Count);

            if (dryRun)
                return new SyncResult(oseonCategories.Count, 0, 0);

            await using var wmsConn = new SqlConnection(wmsConnection);
            await wmsConn.OpenAsync(ct);

            // Step 2: Upsert categories
            foreach (var (name, bemerkung, typ) in oseonCategories)
            {
                try
                {
                    await using var upsertCmd = new SqlCommand(@"
                        IF EXISTS (SELECT 1 FROM [ArticleCategories] WHERE [Name] = @Name)
                        BEGIN
                            UPDATE [ArticleCategories]
                            SET [Description] = @Description, [OseonTyp] = @OseonTyp, [Source] = 'OSEON',
                                [ModifiedAt] = GETDATE(), [ModifiedBy] = 'OseonSync', [ModifiedByWindows] = 'SYSTEM'
                            WHERE [Name] = @Name;
                            SELECT 0; -- updated
                        END
                        ELSE
                        BEGIN
                            INSERT INTO [ArticleCategories] ([Name], [Description], [OseonTyp], [Source], [CreatedAt], [CreatedBy], [CreatedByWindows])
                            VALUES (@Name, @Description, @OseonTyp, 'OSEON', GETDATE(), 'OseonSync', 'SYSTEM');
                            SELECT 1; -- inserted
                        END", wmsConn) { CommandTimeout = 30 };

                    upsertCmd.Parameters.AddWithValue("@Name", name);
                    upsertCmd.Parameters.AddWithValue("@Description", (object?)bemerkung ?? DBNull.Value);
                    upsertCmd.Parameters.AddWithValue("@OseonTyp", (object?)typ ?? DBNull.Value);

                    var result = (int)(await upsertCmd.ExecuteScalarAsync(ct))!;
                    if (result == 1) inserted++;
                    else updated++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(ex, "Fehler beim Upsert der Kategorie '{Name}'.", name);
                }
            }

            // Step 3: Read article-category assignments from OSEON
            var articleCategories = new List<(string ArticleNumber, string CategoryName)>();
            await using (var oseonConn2 = new SqlConnection(oseonConnection))
            {
                await oseonConn2.OpenAsync(ct);
                await using var cmd2 = new SqlCommand(
                    "SELECT CAST(Name AS nvarchar(100)) AS Artikelnummer, CAST(Kategorie AS nvarchar(200)) AS Artikelkategorie FROM Artikel WHERE Kategorie IS NOT NULL AND Kategorie != ''",
                    oseonConn2) { CommandTimeout = 120 };
                await using var reader2 = await cmd2.ExecuteReaderAsync(ct);
                while (await reader2.ReadAsync(ct))
                {
                    var artNr = reader2.GetString(0).Trim();
                    var catName = reader2.GetString(1).Trim();
                    if (!string.IsNullOrEmpty(artNr) && !string.IsNullOrEmpty(catName))
                        articleCategories.Add((artNr, catName));
                }
            }

            _logger.LogInformation("OSEON-Artikel-Zuordnungen: {Count} Zuordnungen gelesen.", articleCategories.Count);

            // Step 4: Bulk-update article category assignments
            var assignUpdated = 0;
            await using var assignCmd = new SqlCommand(@"
                UPDATE a SET a.[ArticleCategoryId] = c.[Id]
                FROM [Articles] a
                INNER JOIN [ArticleCategories] c ON c.[Name] = @CategoryName COLLATE Latin1_General_CI_AS
                WHERE a.[ArticleNumber] = @ArticleNumber COLLATE Latin1_General_CI_AS
                  AND (a.[ArticleCategoryId] IS NULL OR a.[ArticleCategoryId] != c.[Id])",
                wmsConn) { CommandTimeout = 30 };

            assignCmd.Parameters.Add("@ArticleNumber", System.Data.SqlDbType.NVarChar, 100);
            assignCmd.Parameters.Add("@CategoryName", System.Data.SqlDbType.NVarChar, 200);

            foreach (var (artNr, catName) in articleCategories)
            {
                try
                {
                    assignCmd.Parameters["@ArticleNumber"].Value = artNr;
                    assignCmd.Parameters["@CategoryName"].Value = catName;
                    var rows = await assignCmd.ExecuteNonQueryAsync(ct);
                    if (rows > 0) assignUpdated++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(ex, "Fehler beim Zuordnen der Kategorie '{Category}' zu Artikel '{Article}'.", catName, artNr);
                }
            }

            _logger.LogInformation("Artikel-Kategorie-Zuordnungen: {Updated} aktualisiert.", assignUpdated);
            updated += assignUpdated;
        }
        catch (Exception ex)
        {
            errors++;
            errorDetails = ex.Message;
            _logger.LogError(ex, "Fehler beim OSEON-Artikelkategorie-Sync.");
        }

        return new SyncResult(inserted, updated, errors, errorDetails);
    }
```

### Edit: `IDEALAKEWMSService/appsettings.json` — Add toggle

Find the `Sync` section and add:

**After existing Sync entries (e.g., `"EnaioDmsEnabled": false`):**

Add:
```json
"OseonArticleCategoryEnabled": false
```

### Edit: `IDEALAKEWMSService/Workers/SyncWorker.cs` — Add category sync call

**After the Artikel sync block (after the closing `}` of the `Sync:ArticlesEnabled` block), add:**

**Old:**
```csharp
                // OSEON Tracking sync + Werkbank-Sync
                if (_configuration.GetValue<bool>("Sync:OseonTrackingEnabled", false))
```

**New:**
```csharp
                // OSEON Artikelkategorie-Sync (muss nach Artikel-Import laufen)
                if (_configuration.GetValue<bool>("Sync:OseonArticleCategoryEnabled", false))
                {
                    var oseonSync = scope.ServiceProvider.GetRequiredService<IOseonSyncService>();

                    _logger.LogInformation("OSEON-Artikelkategorie-Sync startet...");
                    var catResult = await oseonSync.SyncArticleCategoriesToWmsAsync(dryRun, stoppingToken);
                    _logger.LogInformation(
                        "OSEON-Artikelkategorie-Sync: {Inserted} neu, {Updated} aktualisiert, {Errors} Fehler.{Details}",
                        catResult.Inserted, catResult.Updated, catResult.Errors,
                        catResult.ErrorDetails != null ? $" Details: {catResult.ErrorDetails}" : "");
                }

                // OSEON Tracking sync + Werkbank-Sync
                if (_configuration.GetValue<bool>("Sync:OseonTrackingEnabled", false))
```

### Commands

```bash
cd /c/Entwicklung/C#/AKEBDELight
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests
```

### Commit

```
feat: add OSEON article category sync with toggle and SyncWorker integration
```

---

## Task 10: Documentation + Version

- [ ] Update `CLAUDE.md` with new entities, repositories, fallstricke
- [ ] Update `IdealAkeWms/Views/Help/Changelog.cshtml`
- [ ] Bump `IdealAkeWms/AppVersion.cs` to 1.4.0
- [ ] Bump `IDEALAKEWMSService/AppVersion.cs` to 1.4.0
- [ ] Commit

### Edit: `IdealAkeWms/AppVersion.cs`

**Old:**
```csharp
    public const string Version = "1.3.0";
    public const string Date = "2026-04-03";
```

**New:**
```csharp
    public const string Version = "1.4.0";
    public const string Date = "2026-04-04";
```

### Edit: `IDEALAKEWMSService/AppVersion.cs`

**Old:**
```csharp
    public const string Version = "1.3.0";
    public const string Date = "2026-04-03";
```

**New:**
```csharp
    public const string Version = "1.4.0";
    public const string Date = "2026-04-04";
```

### Edit: `CLAUDE.md` — Add to "Wichtige Dateien" section

Add the following entries:

```markdown
- `Models/ArticleCategory.cs` — Artikelkategorie-Entity (OSEON-sync oder manuell)
- `Models/ArticleAttributeDefinition.cs` — Merkmal-Definition + Enum `AttributeType` (Boolean/Dropdown)
- `Models/ArticleAttributeOption.cs` — Dropdown-Vorgabewerte (kein AuditableEntity)
- `Models/ArticleAttributeValue.cs` — EAV-Wert pro Artikel pro Merkmal
- `Data/Repositories/ArticleCategoryRepository.cs` — CRUD + Batch-Lookup fuer Kategorien
- `Data/Repositories/ArticleAttributeRepository.cs` — Definitionen, Optionen, Werte (EAV-Pattern)
- `Controllers/ArticleCategoriesController.cs` — CRUD Stammdaten (RequireMasterDataAccess)
- `Controllers/ArticleAttributesController.cs` — CRUD Merkmale + Dropdown-Optionen
- `Views/ArticleCategories/Index.cshtml` — Kategorien-Verwaltung
- `Views/ArticleAttributes/Index.cshtml` — Merkmale-Verwaltung mit Optionen-Details
- `Models/ViewModels/ArticleEditViewModel.cs` — ViewModel fuer Artikel-Edit mit Kategorie + Merkmalen
- `SQL/39_AddArticleCategoriesAndAttributes.sql` — Migration fuer 4 neue Tabellen + FK
```

### Edit: `CLAUDE.md` — Add to "Bekannte Fallstricke" section

Add:

```markdown
- **ArticleAttributeOption kein AuditableEntity**: `ArticleAttributeOption` hat nur `Id`, `FK`, `Value`, `SortOrder` — kein `CreatedAt`/`CreatedBy`. Audit-Tracking auf Definition-Ebene reicht
- **Merkmal-Typ nicht aenderbar**: `AttributeType` (Boolean/Dropdown) kann nach Erstellung nicht geaendert werden. Altes deaktivieren, neues erstellen
- **Artikel-Index Merkmal-Performance**: Merkmal-Werte werden per Batch-Query geladen (`GetValuesByArticleIdsAsync`), nicht per Artikel — kein N+1
- **BOM zeigt nur Kategorie**: In der Stueckliste (BOM) werden nur Kategorien angezeigt, keine Merkmale (zu viele Spalten)
- **OSEON Kategorie-Sync Reihenfolge**: Kategorie-Sync muss NACH dem Sage-Artikel-Import laufen, damit Artikel existieren
```

### Edit: `CLAUDE.md` — Add to "AppSettings (DB-Tabelle)" section — No changes needed (no new DB AppSettings)

### Edit: `CLAUDE.md` — Add to "Service-Konfiguration (appsettings.json)" section

Add row:

```markdown
| `Sync:OseonArticleCategoryEnabled` | `false` | OSEON-Artikelkategorie-Sync aktiv |
```

### Edit: `IdealAkeWms/Views/Help/Changelog.cshtml` — Add v1.4.0 entry

Add at the top of the changelog entries (after the page header):

```html
<div class="card mb-3">
    <div class="card-header">
        <strong>Version 1.4.0</strong> <small class="text-muted">04.04.2026</small>
    </div>
    <div class="card-body">
        <ul class="mb-0">
            <li><strong>Artikelkategorien:</strong> Neue Stammdaten-Seite zur Verwaltung von Artikelkategorien. Kategorien koennen manuell erstellt oder per OSEON-Sync automatisch angelegt werden.</li>
            <li><strong>Artikelmerkmale:</strong> Frei definierbare Merkmale (Boolean/Dropdown) pro Artikel. Merkmale werden in der Artikel-Uebersicht als filterbare Spalten angezeigt.</li>
            <li><strong>Artikel-Bearbeitung:</strong> Kategorie-Dropdown und dynamische Merkmal-Felder in der Artikel-Bearbeitungsseite.</li>
            <li><strong>Stueckliste:</strong> Neue Spalte "Kategorie" zeigt die Artikelkategorie jedes Bauteils an.</li>
            <li><strong>OSEON-Sync:</strong> Artikelkategorien koennen automatisch aus OSEON synchronisiert werden (Toggle: Service-Einstellungen).</li>
        </ul>
    </div>
</div>
```

### Commands

```bash
cd /c/Entwicklung/C#/AKEBDELight
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests
```

### Commit

```
docs: bump version to 1.4.0, update CLAUDE.md, changelog for article categories and attributes
```
