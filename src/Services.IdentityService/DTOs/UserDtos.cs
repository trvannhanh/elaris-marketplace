namespace Services.IdentityService.DTOs
{

    // ==================== PROFILE DTOs ====================
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public string? Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsVerified { get; set; }
        public bool IsBanned { get; set; }
    }

    public class SellerProfileDto : UserProfileDto
    {
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public int TotalProducts { get; set; }
        public int TotalSales { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? BusinessName { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    // ==================== SELLER VERIFICATION DTOs ====================
    public class VerificationRequestDto
    {
        public string BusinessName { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string BusinessAddress { get; set; } = string.Empty;
        public IFormFile? BusinessLicense { get; set; }
        public IFormFile? IdCard { get; set; }
    }

    public class ApproveVerificationDto
    {
        public string? Note { get; set; }
    }

    public class RejectVerificationDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    // ==================== PAYOUT DTOs ====================
    public class PayoutInfoDto
    {
        public decimal TotalEarnings { get; set; }
        public decimal Balance { get; set; }
        public decimal AvailableForPayout { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountHolder { get; set; }
        public DateTime? LastPayoutDate { get; set; }
    }

    public class CreatePayoutRequestDto
    {
        public decimal Amount { get; set; }
        public string BankAccount { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankAccountHolder { get; set; } = string.Empty;
    }

    public class PayoutRequestDto
    {
        public Guid Id { get; set; }
        public string SellerId { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BankAccount { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessedBy { get; set; }
    }

    public class ApprovePayoutDto
    {
        public string? Note { get; set; }
    }

    public class RejectPayoutDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    // ==================== ADMIN DTOs ====================
    public class UserDetailDto : UserProfileDto
    {
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Seller specific
        public decimal Balance { get; set; }
        public decimal TotalEarnings { get; set; }
        public int TotalProducts { get; set; }
        public int TotalSales { get; set; }

        // Ban info
        public string? BanReason { get; set; }
        public DateTime? BannedUntil { get; set; }
        public string? BannedBy { get; set; }

        // Verification info
        public bool VerificationRequested { get; set; }
        public string? VerificationStatus { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    public class BanUserDto
    {
        public string Reason { get; set; } = string.Empty;
        public int? DurationDays { get; set; }  // null = permanent
    }

    public class UserStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int TotalBuyers { get; set; }
        public int TotalSellers { get; set; }
        public int VerifiedSellers { get; set; }
        public int ActiveUsers { get; set; }
        public int BannedUsers { get; set; }
        public int PendingVerifications { get; set; }
        public int PendingPayouts { get; set; }
    }
}

