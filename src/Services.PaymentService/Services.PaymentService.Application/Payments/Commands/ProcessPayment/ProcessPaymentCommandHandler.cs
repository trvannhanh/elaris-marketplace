using MediatR;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Commands.ProcessPayment
{
    public class ProcessPaymentCommandHandler
            : IRequestHandler<ProcessPaymentCommand, PaymentDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ILogger<ProcessPaymentCommandHandler> _logger;

        public ProcessPaymentCommandHandler(
            IUnitOfWork uow,
            IPaymentGateway paymentGateway,
            ILogger<ProcessPaymentCommandHandler> logger)
        {
            _uow = uow;
            _paymentGateway = paymentGateway;
            _logger = logger;
        }

        public async Task<PaymentDto> Handle(
            ProcessPaymentCommand request,
            CancellationToken cancellationToken)
        {
            var payment = await _uow.Payment.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
            {
                throw new InvalidOperationException($"Payment {request.PaymentId} not found");
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                throw new InvalidOperationException(
                    $"Payment {request.PaymentId} cannot be processed. Current status: {payment.Status}");
            }

            _logger.LogInformation(
                "[ProcessPayment] Processing payment {PaymentId}", request.PaymentId);

            // Update status to Processing
            payment.Status = PaymentStatus.Processing;
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
                        "[ProcessPayment] Payment {PaymentId} completed. TransactionId: {TransactionId}",
                        payment.Id, result.TransactionId);
                }
                else
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.FailureReason = result.ErrorMessage;

                    _logger.LogWarning(
                        "[ProcessPayment] Payment {PaymentId} failed: {Reason}",
                        payment.Id, result.ErrorMessage);
                }

                payment.UpdatedAt = DateTime.UtcNow;
                await _uow.Payment.UpdateAsync(payment, cancellationToken);

                // Add history
                var history = new PaymentHistory
                {
                    Id = Guid.NewGuid(),
                    PaymentId = payment.Id,
                    Action = result.Success ? "Completed" : "Failed",
                    ChangedBy = "System",
                    Note = result.Success
                        ? $"Payment processed successfully. TransactionId: {result.TransactionId}"
                        : $"Payment failed: {result.ErrorMessage}",
                    CreatedAt = DateTime.UtcNow
                };
                await _uow.Payment.AddHistoryAsync(history, cancellationToken);

                await _uow.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[ProcessPayment] Error processing payment {PaymentId}", payment.Id);

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
