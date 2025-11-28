

namespace Services.PaymentService.Application.DTOs
{
    public class PaymentWebhookDto
    {
        public Guid PaymentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string Signature { get; set; } = string.Empty;
        public Dictionary<string, string>? Data { get; set; }
    }
}
