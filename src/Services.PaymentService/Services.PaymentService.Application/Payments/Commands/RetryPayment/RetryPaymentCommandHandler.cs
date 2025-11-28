using MediatR;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Commands.RetryPayment
{
    public class RetryPaymentCommandHandler
        : IRequestHandler<RetryPaymentCommand, PaymentDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ILogger<RetryPaymentCommandHandler> _logger;

        public RetryPaymentCommandHandler(
            IUnitOfWork uow,
            IPaymentGateway paymentGateway,
            ILogger<RetryPaymentCommandHandler> logger)
        {
            _uow = uow;
            _paymentGateway = paymentGateway;
            _logger = logger;
        }

        public async Task<PaymentDto> Handle(
            RetryPaymentCommand request,
            CancellationToken cancellationToken)
        {
            var payment = await _uow.Payment.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
            {
                throw new InvalidOperationException($"Payment {request.PaymentId} not found");
            }

            if (payment.Status != PaymentStatus.Failed)
            {
                throw new InvalidOperationException(
                    $"Only failed payments can be retried. Current status: {payment.Status}");
            }

            _logger.LogInformation(
                "[RetryPayment] Retrying payment {PaymentId}", request.PaymentId);

            // Reset payment status
            payment.Status = PaymentStatus.Processing;
            payment.FailureReason = null;
            payment.UpdatedAt = DateTime.UtcNow;
            await _uow.Payment.UpdateAsync(payment, cancellationToken);

            try
            {
                // Call payment gateway
                var result = await _paymentGateway.ProcessPaymentAsync(
                    payment.Amount,
                    request.AdditionalDetails,
                    cancellationToken);

                if (result.Success)
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.TransactionId = result.TransactionId;
                    payment.ProcessedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "[RetryPayment] Payment {PaymentId} retry successful. TransactionId: {TransactionId}",
                        payment.Id, result.TransactionId);
                }
                else
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.FailureReason = result.ErrorMessage;

                    _logger.LogWarning(
                        "[RetryPayment] Payment {PaymentId} retry failed: {Reason}",
                        payment.Id, result.ErrorMessage);
                }

                payment.UpdatedAt = DateTime.UtcNow;
                await _uow.Payment.UpdateAsync(payment, cancellationToken);

                // Add history
                var history = new PaymentHistory
                {
                    Id = Guid.NewGuid(),
                    PaymentId = payment.Id,
                    Action = result.Success ? "RetrySucceeded" : "RetryFailed",
                    ChangedBy = "System",
                    Note = result.Success
                        ? $"Payment retry succeeded. TransactionId: {result.TransactionId}"
                        : $"Payment retry failed: {result.ErrorMessage}",
                    CreatedAt = DateTime.UtcNow
                };
                await _uow.Payment.AddHistoryAsync(history, cancellationToken);

                await _uow.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[RetryPayment] Error retrying payment {PaymentId}", payment.Id);

                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = ex.Message;
                payment.UpdatedAt = DateTime.UtcNow;
                await _uow.Payment.UpdateAsync(payment, cancellationToken);
                await _uow.SaveChangesAsync(cancellationToken);

                throw;
            }

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
