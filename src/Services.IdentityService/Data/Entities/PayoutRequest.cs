namespace Services.IdentityService.Data.Entities
{
    public class PayoutRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SellerId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BankAccount { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankAccountHolder { get; set; } = string.Empty;
        public PayoutStatus Status { get; set; } = PayoutStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessedBy { get; set; }
        public string? Note { get; set; }
        public string? RejectionReason { get; set; }
    }

    public enum PayoutStatus
    {
        Pending,
        Approved,
        Rejected,
        Completed
    }
}
