using MediatR;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;


namespace Services.PaymentService.Application.Payments.Commands.CancelPayment
{
    public class CancelPaymentCommandHandler
        : IRequestHandler<CancelPaymentCommand, PaymentDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CancelPaymentCommandHandler> _logger;

        public CancelPaymentCommandHandler(
            IUnitOfWork uow,
            ILogger<CancelPaymentCommandHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<PaymentDto> Handle(
            CancelPaymentCommand request,
            CancellationToken cancellationToken)
        {
            var payment = await _uow.Payment.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
            {
                throw new InvalidOperationException($"Payment {request.PaymentId} not found");
            }

            // Only pending or failed payments can be cancelled
            if (payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.Failed)
            {
                throw new InvalidOperationException(
                    $"Payment {request.PaymentId} cannot be cancelled. Current status: {payment.Status}");
            }

            _logger.LogInformation(
                "[CancelPayment] Cancelling payment {PaymentId}, Reason: {Reason}",
                request.PaymentId, request.Reason);

            payment.Status = PaymentStatus.Cancelled;
            payment.CancellationReason = request.Reason;
            payment.CancelledAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            await _uow.Payment.UpdateAsync(payment, cancellationToken);

            // Add history
            var history = new PaymentHistory
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                Action = "Cancelled",
                ChangedBy = request.CancelledBy,
                Note = $"Payment cancelled. Reason: {request.Reason}",
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Payment.AddHistoryAsync(history, cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[CancelPayment] Payment {PaymentId} cancelled successfully", payment.Id);

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
