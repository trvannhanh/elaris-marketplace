using Microsoft.EntityFrameworkCore;
using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Infrastructure.Persistence
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
            : base(options)
        {
        }

        public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
        public DbSet<InventoryHistory> InventoryHistories { get; set; } = null!;
        public DbSet<StockReservation> StockReservations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== INVENTORY ITEM ====================
            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.ProductId)
                    .IsUnique()
                    .HasDatabaseName("IX_InventoryItem_ProductId");

                entity.HasIndex(e => e.SellerId)
                    .HasDatabaseName("IX_InventoryItem_SellerId");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_InventoryItem_Status");

                entity.Property(e => e.ProductId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.SellerId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.Quantity)
                    .IsRequired();

                entity.Property(e => e.ReservedQuantity)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(e => e.AvailableQuantity)
                    .IsRequired();

                entity.Property(e => e.LowStockThreshold)
                    .IsRequired()
                    .HasDefaultValue(10);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            // ==================== INVENTORY HISTORY ====================
            modelBuilder.Entity<InventoryHistory>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.ProductId)
                    .HasDatabaseName("IX_InventoryHistory_ProductId");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_InventoryHistory_CreatedAt");

                entity.HasIndex(e => e.OrderId)
                    .HasDatabaseName("IX_InventoryHistory_OrderId");

                entity.Property(e => e.ProductId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ChangeType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.QuantityBefore)
                    .IsRequired();

                entity.Property(e => e.QuantityAfter)
                    .IsRequired();

                entity.Property(e => e.QuantityChanged)
                    .IsRequired();

                entity.Property(e => e.ChangedBy)
                    .HasMaxLength(450);

                entity.Property(e => e.Note)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            // ==================== STOCK RESERVATION ====================
            modelBuilder.Entity<StockReservation>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.ProductId)
                    .HasDatabaseName("IX_StockReservation_ProductId");

                entity.HasIndex(e => e.OrderId)
                    .IsUnique()
                    .HasDatabaseName("IX_StockReservation_OrderId");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_StockReservation_Status");

                entity.HasIndex(e => new { e.Status, e.ReservedAt })
                    .HasDatabaseName("IX_StockReservation_Status_ReservedAt");

                entity.Property(e => e.ProductId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.OrderId)
                    .IsRequired();

                entity.Property(e => e.Quantity)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(e => e.ReservedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}
