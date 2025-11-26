using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Services.IdentityService.Data.Entities;

namespace Services.IdentityService.Data
{
    public class AppUser : IdentityUser
    {
        // ==================== BASIC INFO ====================
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // ==================== STATUS ====================
        public bool IsVerified { get; set; } = false;
        public bool IsBanned { get; set; } = false;
        public string? BanReason { get; set; }
        public DateTime? BannedUntil { get; set; }
        public string? BannedBy { get; set; }

        // ==================== SELLER SPECIFIC ====================
        public decimal Balance { get; set; } = 0;           // Số dư chờ rút
        public decimal TotalEarnings { get; set; } = 0;     // Tổng thu nhập
        public int TotalProducts { get; set; } = 0;
        public int TotalSales { get; set; } = 0;
        public decimal Rating { get; set; } = 0;
        public int RatingCount { get; set; } = 0;

        // ==================== BANK INFO (for sellers) ====================
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountHolder { get; set; }

        // ==================== VERIFICATION (for sellers) ====================
        public bool VerificationRequested { get; set; } = false;
        public DateTime? VerificationRequestedAt { get; set; }
        public string? VerificationStatus { get; set; }  // Pending, Approved, Rejected
        public string? VerificationRejectionReason { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerifiedBy { get; set; }

        // ==================== BUSINESS INFO (for verified sellers) ====================
        public string? BusinessName { get; set; }
        public string? TaxId { get; set; }
        public string? BusinessAddress { get; set; }
        public string? BusinessLicenseUrl { get; set; }
        public string? IdCardUrl { get; set; }
    }

    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public DbSet<PayoutRequest> PayoutRequests { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure PayoutRequest
            builder.Entity<PayoutRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.HasIndex(e => e.SellerId);
                entity.HasIndex(e => e.Status);
            });

            // Configure AppUser decimal fields
            builder.Entity<AppUser>(entity =>
            {
                entity.Property(e => e.Balance).HasPrecision(18, 2);
                entity.Property(e => e.TotalEarnings).HasPrecision(18, 2);
                entity.Property(e => e.Rating).HasPrecision(3, 2);
            });
        }
    }
}