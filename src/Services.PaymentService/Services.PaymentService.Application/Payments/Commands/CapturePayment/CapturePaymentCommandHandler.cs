using MediatR;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Commands.CapturePayment
{
    public class CapturePaymentCommandHandler
        : IRequestHandler<CapturePaymentCommand, PaymentDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ILogger<CapturePaymentCommandHandler> _logger;

        public CapturePaymentCommandHandler(
            IUnitOfWork uow,
            IPaymentGateway paymentGateway,
            ILogger<CapturePaymentCommandHandler> logger)
        {
            _uow = uow;
            _paymentGateway = paymentGateway;
            _logger = logger;
        }

        public async Task<PaymentDto> Handle(
            CapturePaymentCommand request,
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
                    $"Payment {request.PaymentId} cannot be captured. Current status: {payment.Status}");
            }

            if (string.IsNullOrEmpty(payment.TransactionId))
            {
                throw new InvalidOperationException("TransactionId is required for capture");
            }

            _logger.LogInformation(
                "[CapturePayment] Capturing payment {PaymentId}, Amount {Amount}",
                request.PaymentId, request.Amount);

            var result = await _paymentGateway.CapturePaymentAsync(
                payment.TransactionId,
                request.Amount,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogError(
                    "[CapturePayment] Failed to capture payment {PaymentId}: {Error}",
                    payment.Id, result.ErrorMessage);
                throw new InvalidOperationException($"Capture failed: {result.ErrorMessage}");
            }

            payment.CapturedAt = DateTime.UtcNow;
            payment.CapturedBy = request.CapturedBy;
            payment.UpdatedAt = DateTime.UtcNow;

            await _uow.Payment.UpdateAsync(payment, cancellationToken);

            var history = new PaymentHistory
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                Action = "Captured",
                ChangedBy = request.CapturedBy,
                Note = $"Payment captured. Amount: {request.Amount}",
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Payment.AddHistoryAsync(history, cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[CapturePayment] Payment {PaymentId} captured successfully", payment.Id);

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
