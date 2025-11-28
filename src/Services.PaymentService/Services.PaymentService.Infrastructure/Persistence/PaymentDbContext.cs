
using Microsoft.EntityFrameworkCore;
using Services.PaymentService.Domain.Entities;


namespace Services.PaymentService.Infrastructure.Persistence
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
            : base(options)
        {
        }

        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<PaymentHistory> PaymentHistories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== PAYMENT ====================
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.OrderId)
                    .IsUnique()
                    .HasDatabaseName("IX_Payment_OrderId");

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_Payment_UserId");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_Payment_Status");

                entity.HasIndex(e => e.TransactionId)
                    .HasDatabaseName("IX_Payment_TransactionId");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_Payment_CreatedAt");

                entity.HasIndex(e => new { e.Status, e.CreatedAt })
                    .HasDatabaseName("IX_Payment_Status_CreatedAt");

                entity.Property(e => e.OrderId)
                    .IsRequired();

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.Amount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(e => e.TransactionId)
                    .HasMaxLength(200);

                entity.Property(e => e.FailureReason)
                    .HasMaxLength(500);

                entity.Property(e => e.CancellationReason)
                    .HasMaxLength(500);

                entity.Property(e => e.RefundReason)
                    .HasMaxLength(500);

                entity.Property(e => e.RefundedAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.CapturedBy)
                    .HasMaxLength(450);

                entity.Property(e => e.RefundedBy)
                    .HasMaxLength(450);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            // ==================== PAYMENT HISTORY ====================
            modelBuilder.Entity<PaymentHistory>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.PaymentId)
                    .HasDatabaseName("IX_PaymentHistory_PaymentId");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_PaymentHistory_CreatedAt");

                entity.Property(e => e.PaymentId)
                    .IsRequired();

                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.ChangedBy)
                    .HasMaxLength(450);

                entity.Property(e => e.Note)
                    .HasMaxLength(1000);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                // Relationship
                entity.HasOne<Payment>()
                    .WithMany()
                    .HasForeignKey(e => e.PaymentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
