

namespace Services.PaymentService.Domain.Entities
{
    public class PaymentHistory
    {
        public Guid Id { get; set; }
        public Guid PaymentId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? ChangedBy { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
