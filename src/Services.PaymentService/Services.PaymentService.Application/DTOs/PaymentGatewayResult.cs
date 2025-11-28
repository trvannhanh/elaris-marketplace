

namespace Services.PaymentService.Application.DTOs
{
    public class PaymentGatewayResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
    }

}
