namespace Services.PaymentService.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string? RefundReason { get; set; }
        public DateTime? RefundedAt { get; set; }
        public string? TransactionId { get; set; } // từ cổng thanh toán
    }

    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed,
        Refunded
    }
}
