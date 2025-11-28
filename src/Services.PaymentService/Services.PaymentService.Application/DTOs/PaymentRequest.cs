using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.DTOs
{
    public class ProcessPaymentRequest
    {
        public Dictionary<string, string>? PaymentDetails { get; set; }
    }

    public class RetryPaymentRequest
    {
        public Dictionary<string, string>? PaymentDetails { get; set; }
    }

    public class CancelPaymentRequest
    {
        public string? Reason { get; set; }
    }

    public class RefundPaymentRequest
    {
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class UpdatePaymentStatusRequest
    {
        public PaymentStatus Status { get; set; }
        public string? Note { get; set; }
    }
}
