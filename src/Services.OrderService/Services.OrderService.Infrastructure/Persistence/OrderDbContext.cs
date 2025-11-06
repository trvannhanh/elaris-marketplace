using MassTransit;
using Microsoft.EntityFrameworkCore;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Infrastructure.Persistence
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.UserId).IsRequired();
                entity.Property(o => o.TotalPrice).HasColumnType("decimal(18,2)");

                entity.HasMany(o => o.Items)
                      .WithOne(i => i.Order)
                      .HasForeignKey(i => i.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Price).HasColumnType("decimal(18,2)");
            });
        }
    }
}
