using Services.PaymentService.Application.DTOs;

namespace Services.PaymentService.Application.Interfaces
{
    public interface IPaymentGateway
    {
        Task<PaymentGatewayResult> ProcessPaymentAsync(
            decimal amount,
            Dictionary<string, string>? details,
            CancellationToken ct);

        Task<PaymentGatewayResult> CapturePaymentAsync(
            string transactionId,
            decimal amount,
            CancellationToken ct);

        Task<PaymentGatewayResult> RefundPaymentAsync(
            string transactionId,
            decimal amount,
            CancellationToken ct);

        bool VerifyWebhookSignature(string signature, Dictionary<string, string>? data);
    }
}
