using MediatR;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Commands.UpdatePaymentStatus
{
    public class UpdatePaymentStatusCommandHandler
        : IRequestHandler<UpdatePaymentStatusCommand, PaymentDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<UpdatePaymentStatusCommandHandler> _logger;

        public UpdatePaymentStatusCommandHandler(
            IUnitOfWork uow,
            ILogger<UpdatePaymentStatusCommandHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<PaymentDto> Handle(
            UpdatePaymentStatusCommand request,
            CancellationToken cancellationToken)
        {
            var payment = await _uow.Payment.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
            {
                throw new InvalidOperationException($"Payment {request.PaymentId} not found");
            }

            var oldStatus = payment.Status;

            _logger.LogInformation(
                "[UpdatePaymentStatus] Updating payment {PaymentId} from {OldStatus} to {NewStatus}",
                request.PaymentId, oldStatus, request.NewStatus);

            payment.Status = request.NewStatus;
            payment.UpdatedAt = DateTime.UtcNow;

            await _uow.Payment.UpdateAsync(payment, cancellationToken);

            // Add history
            var history = new PaymentHistory
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                Action = "StatusUpdated",
                ChangedBy = request.UpdatedBy,
                Note = request.Note ?? $"Status changed from {oldStatus} to {request.NewStatus}",
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Payment.AddHistoryAsync(history, cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[UpdatePaymentStatus] Payment {PaymentId} status updated successfully", payment.Id);

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
