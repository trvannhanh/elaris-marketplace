using MediatR;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Commands.RefundPayment
{
    public class RefundPaymentCommandHandler
        : IRequestHandler<RefundPaymentCommand, PaymentDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ILogger<RefundPaymentCommandHandler> _logger;

        public RefundPaymentCommandHandler(
            IUnitOfWork uow,
            IPaymentGateway paymentGateway,
            ILogger<RefundPaymentCommandHandler> logger)
        {
            _uow = uow;
            _paymentGateway = paymentGateway;
            _logger = logger;
        }

        public async Task<PaymentDto> Handle(
            RefundPaymentCommand request,
            CancellationToken cancellationToken)
        {
            var payment = await _uow.Payment.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
            {
                throw new InvalidOperationException($"Payment {request.PaymentId} not found");
            }

            if (payment.Status != PaymentStatus.Completed)
            {
                throw new InvalidOperationException(
                    $"Only completed payments can be refunded. Current status: {payment.Status}");
            }

            if (string.IsNullOrEmpty(payment.TransactionId))
            {
                throw new InvalidOperationException("TransactionId is required for refund");
            }

            // Validate refund amount
            var alreadyRefunded = payment.RefundedAmount ?? 0;
            var availableForRefund = payment.Amount - alreadyRefunded;

            if (request.Amount > availableForRefund)
            {
                throw new InvalidOperationException(
                    $"Refund amount {request.Amount} exceeds available amount {availableForRefund}");
            }

            _logger.LogInformation(
                "[RefundPayment] Refunding payment {PaymentId}, Amount {Amount}, Reason: {Reason}",
                request.PaymentId, request.Amount, request.Reason);

            // Call payment gateway
            var result = await _paymentGateway.RefundPaymentAsync(
                payment.TransactionId,
                request.Amount,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogError(
                    "[RefundPayment] Failed to refund payment {PaymentId}: {Error}",
                    payment.Id, result.ErrorMessage);
                throw new InvalidOperationException($"Refund failed: {result.ErrorMessage}");
            }

            // Update payment
            var newRefundedAmount = alreadyRefunded + request.Amount;
            payment.RefundedAmount = newRefundedAmount;
            payment.RefundReason = request.Reason;
            payment.RefundedAt = DateTime.UtcNow;
            payment.RefundedBy = request.RefundedBy;

            // Determine new status
            if (newRefundedAmount >= payment.Amount)
            {
                payment.Status = PaymentStatus.Refunded;
            }
            else
            {
                payment.Status = PaymentStatus.PartiallyRefunded;
            }

            payment.UpdatedAt = DateTime.UtcNow;
            await _uow.Payment.UpdateAsync(payment, cancellationToken);

            // Add history
            var history = new PaymentHistory
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                Action = payment.Status == PaymentStatus.Refunded ? "Refunded" : "PartiallyRefunded",
                ChangedBy = request.RefundedBy,
                Note = $"Refund amount: {request.Amount}. Reason: {request.Reason}",
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Payment.AddHistoryAsync(history, cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[RefundPayment] Payment {PaymentId} refunded successfully. Status: {Status}",
                payment.Id, payment.Status);

            return MapToDto(payment);
        }

        private PaymentDto MapToDto(Payment payment)
        {
            return new PaymentDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                UserId = payment.UserId,
                Amount = payment.Amount,
                Status = payment.Status.ToString(),
                TransactionId = payment.TransactionId,
                FailureReason = payment.FailureReason,
                CreatedAt = payment.CreatedAt,
                ProcessedAt = payment.ProcessedAt,
                CapturedAt = payment.CapturedAt,
                RefundedAt = payment.RefundedAt,
                RefundedAmount = payment.RefundedAmount,
                RefundReason = payment.RefundReason
            };
        }
    }
}
