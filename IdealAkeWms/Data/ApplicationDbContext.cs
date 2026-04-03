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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
        });

        // StockMovement
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("StockMovements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)").IsRequired();
            entity.Property(e => e.ProductionOrder).HasMaxLength(100);
            entity.Property(e => e.WindowsUser).HasMaxLength(200).IsRequired();
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

            entity.Property(e => e.PickingStatus).HasMaxLength(50);

            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.ArticleNumber);
            entity.HasIndex(e => e.IsDone);
            entity.HasIndex(e => e.ProductionWorkplaceId);

            entity.Property(e => e.ReleasedBy).HasMaxLength(200);
            entity.HasIndex(e => new { e.IsReleasedForPicking, e.IsDone })
                .HasDatabaseName("IX_ProductionOrders_IsReleasedForPicking_IsDone");

            entity.HasOne(e => e.ProductionWorkplace)
                .WithMany(w => w.ProductionOrders)
                .HasForeignKey(e => e.ProductionWorkplaceId)
                .OnDelete(DeleteBehavior.SetNull);
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
    }
}
