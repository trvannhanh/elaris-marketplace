using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Commands.HandleWebhook
{
    public class HandleWebhookCommandHandler
        : IRequestHandler<HandleWebhookCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ILogger<HandleWebhookCommandHandler> _logger;

        public HandleWebhookCommandHandler(
            IUnitOfWork uow,
            IPaymentGateway paymentGateway,
            ILogger<HandleWebhookCommandHandler> logger)
        {
            _uow = uow;
            _paymentGateway = paymentGateway;
            _logger = logger;
        }

        public async Task<bool> Handle(
            HandleWebhookCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("[HandleWebhook] Processing webhook");

            // Verify webhook signature
            if (!_paymentGateway.VerifyWebhookSignature(request.Signature, request.Data))
            {
                _logger.LogWarning("[HandleWebhook] Invalid webhook signature");
                return false;
            }

            // Extract data
            if (!request.Data.TryGetValue("transactionId", out var transactionId) ||
                !request.Data.TryGetValue("status", out var status))
            {
                _logger.LogWarning("[HandleWebhook] Missing required fields");
                return false;
            }

            // Find payment by transaction ID
            var payment = await FindPaymentByTransactionId(transactionId, cancellationToken);
            if (payment == null)
            {
                _logger.LogWarning(
                    "[HandleWebhook] Payment not found for TransactionId: {TransactionId}",
                    transactionId);
                return false;
            }

            _logger.LogInformation(
                "[HandleWebhook] Updating payment {PaymentId} status to {Status}",
                payment.Id, status);

            // Update payment based on webhook status
            var oldStatus = payment.Status;
            payment.Status = MapWebhookStatus(status);
            payment.UpdatedAt = DateTime.UtcNow;

            if (payment.Status == PaymentStatus.Completed && !payment.ProcessedAt.HasValue)
            {
                payment.ProcessedAt = DateTime.UtcNow;
            }

            await _uow.Payment.UpdateAsync(payment, cancellationToken);

            // Add history
            var history = new PaymentHistory
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                Action = "WebhookReceived",
                ChangedBy = "System",
                Note = $"Webhook received. Status changed from {oldStatus} to {payment.Status}",
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Payment.AddHistoryAsync(history, cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[HandleWebhook] Payment {PaymentId} updated successfully", payment.Id);

            return true;
        }

        private async Task<Payment?> FindPaymentByTransactionId(
            string transactionId,
            CancellationToken ct)
        {
            var payments = await _uow.Payment.GetQueryable()
                .Where(p => p.TransactionId == transactionId)
                .ToListAsync(ct);

            return payments.FirstOrDefault();
        }

        private PaymentStatus MapWebhookStatus(string status)
        {
            return status.ToLower() switch
            {
                "completed" or "success" => PaymentStatus.Completed,
                "failed" or "error" => PaymentStatus.Failed,
                "pending" => PaymentStatus.Pending,
                "processing" => PaymentStatus.Processing,
                "refunded" => PaymentStatus.Refunded,
                "cancelled" => PaymentStatus.Cancelled,
                _ => PaymentStatus.Failed
            };
        }
    }
}
