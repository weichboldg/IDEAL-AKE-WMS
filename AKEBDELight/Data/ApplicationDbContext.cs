using AKEBDELight.Models;
using Microsoft.EntityFrameworkCore;

namespace AKEBDELight.Data;

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
    }
}
