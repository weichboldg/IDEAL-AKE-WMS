using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Workstation> Workstations => Set<Workstation>();
    public DbSet<WorkstationUser> WorkstationUsers => Set<WorkstationUser>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<ProductionOrderPickingStatus> ProductionOrderPickingStatuses => Set<ProductionOrderPickingStatus>();
    public DbSet<ProductionOrderBdeStatus> ProductionOrderBdeStatuses => Set<ProductionOrderBdeStatus>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<PickingItem> PickingItems => Set<PickingItem>();
    public DbSet<ProductionWorkplace> ProductionWorkplaces => Set<ProductionWorkplace>();
    public DbSet<ProductionWorkplaceUser> ProductionWorkplaceUsers => Set<ProductionWorkplaceUser>();
    public DbSet<WorkOperation> WorkOperations => Set<WorkOperation>();
    public DbSet<OseonProductionOrder> OseonProductionOrders => Set<OseonProductionOrder>();
    public DbSet<OseonWorkOperation> OseonWorkOperations => Set<OseonWorkOperation>();
    public DbSet<ServiceSetting> ServiceSettings => Set<ServiceSetting>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<OseonOperationConfig> OseonOperationConfigs => Set<OseonOperationConfig>();
    public DbSet<EnaioDmsDocument> EnaioDmsDocuments => Set<EnaioDmsDocument>();
    public DbSet<OrderRecipientGroup> OrderRecipientGroups => Set<OrderRecipientGroup>();
    public DbSet<OrderRecipient> OrderRecipients => Set<OrderRecipient>();
    public DbSet<ArticleGroupRecipientMapping> ArticleGroupRecipientMappings => Set<ArticleGroupRecipientMapping>();
    public DbSet<PartRequisition> PartRequisitions => Set<PartRequisition>();
    public DbSet<ArticleCategory> ArticleCategories => Set<ArticleCategory>();
    public DbSet<ArticleAttributeDefinition> ArticleAttributeDefinitions => Set<ArticleAttributeDefinition>();
    public DbSet<ArticleAttributeOption> ArticleAttributeOptions => Set<ArticleAttributeOption>();
    public DbSet<ArticleAttributeValue> ArticleAttributeValues => Set<ArticleAttributeValue>();
    public DbSet<BdeActivity> BdeActivities => Set<BdeActivity>();
    public DbSet<BdeBooking> BdeBookings => Set<BdeBooking>();
    public DbSet<BdeBookingQuantity> BdeBookingQuantities => Set<BdeBookingQuantity>();
    public DbSet<BdeOperator> BdeOperators => Set<BdeOperator>();
    public DbSet<BdeShift> BdeShifts => Set<BdeShift>();
    public DbSet<BdeTerminal> BdeTerminals => Set<BdeTerminal>();
    public DbSet<CachedBomHeader> CachedBomHeaders => Set<CachedBomHeader>();
    public DbSet<CachedBomItem> CachedBomItems => Set<CachedBomItem>();
    public DbSet<UserViewPreference> UserViewPreferences => Set<UserViewPreference>();
    public DbSet<WarehouseRequisition> WarehouseRequisitions => Set<WarehouseRequisition>();
    public DbSet<WarehouseRequisitionItem> WarehouseRequisitionItems => Set<WarehouseRequisitionItem>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<WorkStep> WorkSteps => Set<WorkStep>();
    public DbSet<FaWorkStep> FaWorkSteps => Set<FaWorkStep>();
    public DbSet<FaWorkStepSpec> FaWorkStepSpecs => Set<FaWorkStepSpec>();
    public DbSet<FaAttributeDefinition> FaAttributeDefinitions => Set<FaAttributeDefinition>();
    public DbSet<FaAttributeOption> FaAttributeOptions => Set<FaAttributeOption>();
    public DbSet<FaAttributeWorkStep> FaAttributeWorkSteps => Set<FaAttributeWorkStep>();
    public DbSet<FaAttributeValue> FaAttributeValues => Set<FaAttributeValue>();
    public DbSet<ProductionWorkplaceWorkStep> ProductionWorkplaceWorkSteps => Set<ProductionWorkplaceWorkStep>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("dbo");

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PersonalNumber).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasOne(e => e.DefaultWorkStep)
                .WithMany()
                .HasForeignKey(e => e.DefaultWorkStepId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.DefaultWorkplace)
                .WithMany()
                .HasForeignKey(e => e.DefaultWorkplaceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.AdGroup).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.Key).IsUnique();
        });

        // UserRole
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Workstation
        modelBuilder.Entity<Workstation>(entity =>
        {
            entity.ToTable("Workstations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasOne(e => e.DefaultUser)
                .WithMany(u => u.DefaultWorkstations)
                .HasForeignKey(e => e.DefaultUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // WorkstationUser
        modelBuilder.Entity<WorkstationUser>(entity =>
        {
            entity.ToTable("WorkstationUsers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => new { e.WorkstationId, e.UserId }).IsUnique();

            entity.HasOne(e => e.Workstation)
                .WithMany(w => w.WorkstationUsers)
                .HasForeignKey(e => e.WorkstationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.WorkstationUsers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StorageLocation
        modelBuilder.Entity<StorageLocation>(entity =>
        {
            entity.ToTable("StorageLocations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Zone).HasMaxLength(100);
            entity.Property(e => e.Capacity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BarcodeValue).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.Property(e => e.Source).HasMaxLength(20).IsRequired().HasDefaultValue(StorageLocationSource.Manual);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Source);
            entity.Property(e => e.IstBuchbar).HasDefaultValue(true);
            entity.HasIndex(e => e.IstBuchbar);

            entity.HasIndex(e => e.Code).IsUnique();
        });

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

            // NoAction (statt SetNull) — verhindert SQL Server "multiple cascade paths" Fehler.
            // Article→Values und Definition→Values sind bereits Cascade. UI verhindert Loeschen
            // einer Option die in Verwendung ist (OptionIsInUseAsync).
            entity.HasOne(e => e.SelectedOption)
                .WithMany()
                .HasForeignKey(e => e.SelectedOptionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // StockMovement
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("StockMovements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)").IsRequired();
            entity.Property(e => e.ProductionOrder).HasMaxLength(100);
            entity.Property(e => e.WindowsUser).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.ArticleId);
            entity.HasIndex(e => e.StorageLocationId);
            entity.HasIndex(e => e.Timestamp);

            entity.HasOne(e => e.Article)
                .WithMany(a => a.StockMovements)
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.StorageLocation)
                .WithMany(sl => sl.StockMovements)
                .HasForeignKey(e => e.StorageLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User)
                .WithMany(u => u.StockMovements)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.SourceStorageLocation)
                .WithMany()
                .HasForeignKey(e => e.SourceStorageLocationId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasIndex(e => e.SourceStorageLocationId);

            // Performance: Stock calculations always group by (ArticleId, StorageLocationId)
            entity.HasIndex(e => new { e.ArticleId, e.StorageLocationId })
                .HasDatabaseName("IX_StockMovements_ArticleId_StorageLocationId");

            // Performance: Source deduction queries filter on all three columns
            entity.HasIndex(e => new { e.ArticleId, e.SourceStorageLocationId, e.MovementType })
                .HasDatabaseName("IX_StockMovements_ArticleId_SourceStorageLocationId_MovementType");

            // Performance: FA-filter in Bestandsuebersicht and Bewegungshistorie
            entity.HasIndex(e => e.ProductionOrder)
                .HasDatabaseName("IX_StockMovements_ProductionOrder");
        });

        // ProductionOrder
        modelBuilder.Entity<ProductionOrder>(entity =>
        {
            entity.ToTable("ProductionOrders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
            entity.Property(e => e.Customer).HasMaxLength(200);
            entity.Property(e => e.ArticleNumber).HasMaxLength(100);
            entity.Property(e => e.Description1).HasMaxLength(500);
            entity.Property(e => e.Description2).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.ArticleNumber);
            entity.HasIndex(e => e.IsDone);
            entity.HasIndex(e => e.ProductionWorkplaceId);

            entity.HasOne(e => e.ProductionWorkplace)
                .WithMany(w => w.ProductionOrders)
                .HasForeignKey(e => e.ProductionWorkplaceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ProductionOrderPickingStatus (Phase 1 — Spec 5.2)
        modelBuilder.Entity<ProductionOrderPickingStatus>(entity =>
        {
            entity.ToTable("ProductionOrderPickingStatus");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PickingStatus).HasMaxLength(50);
            entity.Property(e => e.ReleasedBy).HasMaxLength(200);
            entity.Property(e => e.AssignedPickerName).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.ProductionOrderId).IsUnique()
                .HasDatabaseName("UQ_ProductionOrderPickingStatus_ProductionOrderId");
            entity.HasIndex(e => e.IsReleasedForPicking)
                .HasDatabaseName("IX_ProductionOrderPickingStatus_IsReleasedForPicking");
            entity.HasIndex(e => e.AssignedPickerId)
                .HasFilter("[AssignedPickerId] IS NOT NULL")
                .HasDatabaseName("IX_ProductionOrderPickingStatus_AssignedPickerId");

            entity.HasOne(e => e.ProductionOrder)
                .WithOne(p => p.PickingStatus)
                .HasForeignKey<ProductionOrderPickingStatus>(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AssignedPicker)
                .WithMany()
                .HasForeignKey(e => e.AssignedPickerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ProductionOrderBdeStatus (Phase 1 — Spec 5.2)
        modelBuilder.Entity<ProductionOrderBdeStatus>(entity =>
        {
            entity.ToTable("ProductionOrderBdeStatus");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.ProductionOrderId).IsUnique()
                .HasDatabaseName("UQ_ProductionOrderBdeStatus_ProductionOrderId");

            entity.HasOne(e => e.ProductionOrder)
                .WithOne(p => p.BdeStatus)
                .HasForeignKey<ProductionOrderBdeStatus>(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductionOrderAssemblyGroups + ProductionOrderAssemblyGroupSpecs entfernt in
        // v1.22.0 — ersetzt durch FaWorkSteps/FaWorkStepSpecs (daten-erhaltende Migration
        // FaWorkStepsAndAttributes, siehe Spec 2026-06-12).

        // ProductionWorkplaceAssemblyGroup (Phase-1-Platzhalter, nie im Code genutzt) entfernt
        // in v1.22.0 — ersetzt durch ProductionWorkplaceWorkSteps (N:M Werkbank↔Arbeitsgang).
        // Die DB-Tabelle wird von der Migration FaWorkStepsAndAttributes per guarded Sql
        // gedroppt (nicht via Snapshot-Diff, da sie nicht mehr im EF-Model enthalten ist).

        // WorkStep (FA-Vorbau v1.22.0)
        modelBuilder.Entity<WorkStep>(entity =>
        {
            entity.ToTable("WorkSteps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SearchString).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.Code).IsUnique();
        });

        // FaWorkStep (FA-Vorbau v1.22.0)
        modelBuilder.Entity<FaWorkStep>(entity =>
        {
            entity.ToTable("FaWorkSteps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Source).HasMaxLength(20);
            entity.Property(e => e.CompletedBy).HasMaxLength(200);
            entity.Property(e => e.SpecCompletedBy).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => new { e.ProductionOrderId, e.WorkStepId }).IsUnique();
            entity.HasIndex(e => e.WorkStepId);
            entity.HasIndex(e => new { e.ProductionOrderId, e.IsRemoved });

            entity.HasOne(e => e.ProductionOrder)
                .WithMany()
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.WorkStep)
                .WithMany()
                .HasForeignKey(e => e.WorkStepId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // FaWorkStepSpec (FA-Vorbau v1.22.0)
        modelBuilder.Entity<FaWorkStepSpec>(entity =>
        {
            entity.ToTable("FaWorkStepSpecs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
            entity.Property(e => e.Notes).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.FaWorkStepId);

            entity.HasOne(e => e.FaWorkStep)
                .WithMany(f => f.Specs)
                .HasForeignKey(e => e.FaWorkStepId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Article)
                .WithMany()
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // FaAttributeDefinition (FA-Vorbau v1.22.0)
        modelBuilder.Entity<FaAttributeDefinition>(entity =>
        {
            entity.ToTable("FaAttributeDefinitions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);
        });

        // FaAttributeOption (FA-Vorbau v1.22.0)
        modelBuilder.Entity<FaAttributeOption>(entity =>
        {
            entity.ToTable("FaAttributeOptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.FaAttributeDefinitionId);

            entity.HasOne(e => e.Definition)
                .WithMany(d => d.Options)
                .HasForeignKey(e => e.FaAttributeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // FaAttributeWorkStep (FA-Vorbau v1.22.0, N:M Junction)
        modelBuilder.Entity<FaAttributeWorkStep>(entity =>
        {
            entity.ToTable("FaAttributeWorkSteps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => new { e.FaAttributeDefinitionId, e.WorkStepId }).IsUnique();

            entity.HasOne(e => e.Definition)
                .WithMany(d => d.WorkSteps)
                .HasForeignKey(e => e.FaAttributeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.WorkStep)
                .WithMany()
                .HasForeignKey(e => e.WorkStepId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // FaAttributeValue (FA-Vorbau v1.22.0)
        modelBuilder.Entity<FaAttributeValue>(entity =>
        {
            entity.ToTable("FaAttributeValues");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);
            entity.Property(e => e.TextValue).HasMaxLength(1000);

            entity.HasIndex(e => new { e.ProductionOrderId, e.FaAttributeDefinitionId }).IsUnique();

            entity.HasOne(e => e.ProductionOrder)
                .WithMany()
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Definition)
                .WithMany()
                .HasForeignKey(e => e.FaAttributeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.SelectedOption)
                .WithMany()
                .HasForeignKey(e => e.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProductionWorkplaceWorkStep (FA-Vorbau v1.22.0, N:M Junction)
        modelBuilder.Entity<ProductionWorkplaceWorkStep>(entity =>
        {
            entity.ToTable("ProductionWorkplaceWorkSteps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => new { e.ProductionWorkplaceId, e.WorkStepId }).IsUnique();

            entity.HasOne(e => e.ProductionWorkplace)
                .WithMany()
                .HasForeignKey(e => e.ProductionWorkplaceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.WorkStep)
                .WithMany()
                .HasForeignKey(e => e.WorkStepId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AppSetting
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.ToTable("AppSettings");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Value).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Holiday
        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.ToTable("Holidays");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).HasColumnType("date");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Source).HasDefaultValue(HolidaySource.Manual);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.Date).IsUnique();
        });

        // PickingItem
        modelBuilder.Entity<PickingItem>(entity =>
        {
            entity.ToTable("PickingItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BomArticleNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.BomPosition).HasMaxLength(50);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
            entity.Property(e => e.PickedBy).HasMaxLength(200);
            entity.Property(e => e.PickedByWindows).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.ProductionOrderId);
            entity.HasIndex(e => new { e.ProductionOrderId, e.IsPicked });

            entity.HasOne(e => e.ProductionOrder)
                .WithMany()
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SourceStorageLocation)
                .WithMany()
                .HasForeignKey(e => e.SourceStorageLocationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ProductionWorkplace
        modelBuilder.Entity<ProductionWorkplace>(entity =>
        {
            entity.ToTable("ProductionWorkplaces");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Hall).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);
        });

        // ProductionWorkplaceUser
        modelBuilder.Entity<ProductionWorkplaceUser>(entity =>
        {
            entity.ToTable("ProductionWorkplaceUsers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => new { e.ProductionWorkplaceId, e.UserId }).IsUnique();

            entity.HasOne(e => e.ProductionWorkplace)
                .WithMany(w => w.ProductionWorkplaceUsers)
                .HasForeignKey(e => e.ProductionWorkplaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.ProductionWorkplaceUsers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WorkOperation
        modelBuilder.Entity<WorkOperation>(entity =>
        {
            entity.ToTable("WorkOperations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OperationNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ReportedBy).HasMaxLength(200);
            entity.Property(e => e.ReportedByWindows).HasMaxLength(200);
            entity.Property(e => e.ExternalSource).HasMaxLength(100);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.ProductionOrderId);
            entity.HasIndex(e => new { e.ProductionOrderId, e.Sequence });

            entity.HasOne(e => e.ProductionOrder)
                .WithMany(po => po.WorkOperations)
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ProductionWorkplace)
                .WithMany(w => w.WorkOperations)
                .HasForeignKey(e => e.ProductionWorkplaceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // OseonProductionOrder
        modelBuilder.Entity<OseonProductionOrder>(entity =>
        {
            entity.ToTable("OseonProductionOrders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OseonOrderNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CustomerOrderNumber).HasMaxLength(100);
            entity.Property(e => e.ArticleNumber).HasMaxLength(100);
            entity.Property(e => e.Description1).HasMaxLength(500);
            entity.Property(e => e.Description2).HasMaxLength(500);
            entity.Property(e => e.WorkplaceName).HasMaxLength(200);
            entity.Property(e => e.QuantityTarget).HasColumnType("decimal(18,3)");
            entity.Property(e => e.QuantityActual).HasColumnType("decimal(18,3)");
            entity.Property(e => e.DueDate).HasColumnType("date");
            entity.Property(e => e.LastChangedInOseon).HasColumnType("datetime2");
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.OseonOrderNumber).IsUnique();
            entity.HasIndex(e => e.CustomerOrderNumber);
            entity.HasIndex(e => e.OseonId);
            entity.HasIndex(e => e.OseonStatus);
            entity.HasIndex(e => e.WorkplaceName);
            entity.HasIndex(e => e.ArticleNumber);

            entity.HasOne(e => e.ProductionWorkplace)
                .WithMany()
                .HasForeignKey(e => e.ProductionWorkplaceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // OseonWorkOperation
        modelBuilder.Entity<OseonWorkOperation>(entity =>
        {
            entity.ToTable("OseonWorkOperations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PositionNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.LastStatusReportInOseon).HasColumnType("datetime2");
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.OseonProductionOrderId);
            entity.HasIndex(e => new { e.OseonProductionOrderId, e.PositionNumber }).IsUnique();
            // Covering index for the EXISTS-subquery in OseonProductionOrderRepository.GetPagedAsync
            // when showFinished=false AND relevantOperationNames is supplied:
            // WHERE OseonProductionOrderId = X AND OseonStatus NOT IN (90,95) AND Name IN (...)
            entity.HasIndex(e => new { e.OseonProductionOrderId, e.OseonStatus, e.Name })
                .HasDatabaseName("IX_OseonWorkOperations_OrderStatusName");

            entity.HasOne(e => e.OseonProductionOrder)
                .WithMany(o => o.WorkOperations)
                .HasForeignKey(e => e.OseonProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OseonOperationConfig
        modelBuilder.Entity<OseonOperationConfig>(entity =>
        {
            entity.ToTable("OseonOperationConfigs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OperationName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.HasIndex(e => e.OperationName).IsUnique();
        });

        // EnaioDmsDocument
        modelBuilder.Entity<EnaioDmsDocument>(entity =>
        {
            entity.ToTable("EnaioDmsDocuments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.OrderNumber).HasMaxLength(100);
            entity.HasIndex(e => e.OrderNumber);
            entity.HasIndex(e => e.EnaioDmsObjectId).IsUnique();
        });

        // ServiceSetting
        modelBuilder.Entity<ServiceSetting>(entity =>
        {
            entity.ToTable("ServiceSettings");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Value).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

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

        modelBuilder.Entity<CachedBomHeader>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Artikelnummer).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ContentHash).IsRequired().HasMaxLength(64);
            entity.HasIndex(e => e.Artikelnummer).IsUnique().HasDatabaseName("IX_CachedBomHeaders_Artikelnummer");
        });

        modelBuilder.Entity<CachedBomItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Position).HasMaxLength(50);
            entity.Property(e => e.Baugruppe).HasMaxLength(200);
            entity.Property(e => e.Ressourcenummer).HasMaxLength(100);
            entity.Property(e => e.Bezeichnung1).HasMaxLength(500);
            entity.Property(e => e.Bezeichnung2).HasMaxLength(500);
            entity.Property(e => e.Menge).HasColumnType("decimal(18,3)");
            entity.Property(e => e.Beschaffungsartikel).HasMaxLength(100);
            entity.Property(e => e.Artikelgruppe).HasMaxLength(100);

            entity.HasOne(e => e.CachedBomHeader)
                  .WithMany(h => h.Items)
                  .HasForeignKey(e => e.CachedBomHeaderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.CachedBomHeaderId).HasDatabaseName("IX_CachedBomItems_CachedBomHeaderId");
            entity.HasIndex(e => e.Ressourcenummer).HasDatabaseName("IX_CachedBomItems_Ressourcenummer");
        });

        // UserViewPreference
        modelBuilder.Entity<UserViewPreference>(entity =>
        {
            entity.ToTable("UserViewPreferences");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ViewKey).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SettingsJson).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => new { e.UserId, e.ViewKey }).IsUnique()
                .HasDatabaseName("UQ_UserViewPreferences_User_View");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BDE: Terminals
        modelBuilder.Entity<BdeTerminal>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DefaultProductionWorkplace).WithMany().HasForeignKey(e => e.DefaultProductionWorkplaceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // BDE: Operators
        modelBuilder.Entity<BdeOperator>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.PersonnelNumber).IsUnique();
            entity.HasIndex(e => e.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
        });

        // BDE: Activities
        modelBuilder.Entity<BdeActivity>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // BDE: Bookings
        modelBuilder.Entity<BdeBooking>(entity =>
        {
            entity.HasOne(e => e.BdeOperator).WithMany().HasForeignKey(e => e.BdeOperatorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ProductionWorkplace).WithMany().HasForeignKey(e => e.ProductionWorkplaceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BdeTerminal).WithMany().HasForeignKey(e => e.BdeTerminalId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.WorkOperation).WithMany().HasForeignKey(e => e.WorkOperationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BdeActivity).WithMany().HasForeignKey(e => e.BdeActivityId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ParentBooking).WithMany().HasForeignKey(e => e.ParentBookingId).OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.BookingType).HasConversion<byte>();
            entity.Property(e => e.Status).HasConversion<byte>();

            entity.HasIndex(e => e.WorkOperationId)
                .HasFilter("[EndedAt] IS NULL AND [IsCancelled] = 0 AND [WorkOperationId] IS NOT NULL")
                .HasDatabaseName("IX_BdeBookings_WorkOperationId_Active");

            entity.HasIndex(e => e.BdeOperatorId)
                .HasFilter("[EndedAt] IS NULL AND [IsCancelled] = 0")
                .HasDatabaseName("IX_BdeBookings_BdeOperatorId_Active");

            entity.HasIndex(e => new { e.ProductionWorkplaceId, e.EndedAt })
                .HasDatabaseName("IX_BdeBookings_Workplace_EndedAt");

            entity.HasIndex(e => new { e.BdeOperatorId, e.StartedAt })
                .HasDatabaseName("IX_BdeBookings_Operator_StartedAt");

            entity.HasIndex(e => e.StartedAt)
                .HasDatabaseName("IX_BdeBookings_StartedAt");

            entity.ToTable(t => t.HasCheckConstraint("CK_BdeBookings_Target",
                "([WorkOperationId] IS NOT NULL AND [BdeActivityId] IS NULL) OR ([WorkOperationId] IS NULL AND [BdeActivityId] IS NOT NULL)"));

            entity.ToTable(t => t.HasCheckConstraint("CK_BdeBookings_TypeTarget",
                "([BookingType] = 3 AND [BdeActivityId] IS NOT NULL) OR ([BookingType] IN (1,2) AND [WorkOperationId] IS NOT NULL)"));

            entity.ToTable(t => t.HasCheckConstraint("CK_BdeBookings_StatusEnded",
                "([Status] = 1 AND [EndedAt] IS NULL) OR ([Status] IN (2,3,4,5) AND [EndedAt] IS NOT NULL)"));
        });

        // BDE: Shifts
        modelBuilder.Entity<BdeShift>(entity =>
        {
            entity.HasIndex(e => new { e.ProductionWorkplaceId, e.DayOfWeek })
                .HasDatabaseName("IX_BdeShifts_Workplace_Day");

            entity.HasOne(e => e.ProductionWorkplace)
                .WithMany()
                .HasForeignKey(e => e.ProductionWorkplaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BDE: BookingQuantities
        modelBuilder.Entity<BdeBookingQuantity>(entity =>
        {
            entity.HasOne(e => e.BdeBooking).WithMany(b => b.Quantities).HasForeignKey(e => e.BdeBookingId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.BdeOperator).WithMany().HasForeignKey(e => e.BdeOperatorId).OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.GoodQuantity).HasPrecision(18, 4);
            entity.Property(e => e.ScrapQuantity).HasPrecision(18, 4);

            entity.HasIndex(e => new { e.BdeBookingId, e.ReportedAt })
                .HasDatabaseName("IX_BdeBookingQuantities_Booking_ReportedAt");

            entity.HasIndex(e => e.BdeBookingId)
                .IsUnique()
                .HasFilter("[IsFinal] = 1")
                .HasDatabaseName("IX_BdeBookingQuantities_Booking_Final");
        });

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
            entity.HasIndex(e => e.CreatedByUserId);

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

        // SyncLog
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
    }
}
