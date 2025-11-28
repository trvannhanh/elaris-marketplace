namespace Services.PaymentService.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public string? TransactionId { get; set; }
        public string? FailureReason { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CapturedAt { get; set; }
        public string? CapturedBy { get; set; }
        public DateTime? RefundedAt { get; set; }
        public string? RefundedBy { get; set; }
        public decimal? RefundedAmount { get; set; }
        public string? RefundReason { get; set; }
        public DateTime? CancelledAt { get; set; }
    }

    public enum PaymentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Refunded,
        PartiallyRefunded
    }
}
