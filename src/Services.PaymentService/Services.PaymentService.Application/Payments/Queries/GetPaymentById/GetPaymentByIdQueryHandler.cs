

using MediatR;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Queries.GetPaymentById
{
    public class GetPaymentByIdQueryHandler : IRequestHandler<GetPaymentByIdQuery, PaymentDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetPaymentByIdQueryHandler> _logger;

        public GetPaymentByIdQueryHandler(
            IUnitOfWork uow,
            ILogger<GetPaymentByIdQueryHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<PaymentDto> Handle(
            GetPaymentByIdQuery request,
            CancellationToken cancellationToken)
        {
            var payment = await _uow.Payment.GetByIdAsync(request.PaymentId, cancellationToken);

            if (payment == null)
            {
                throw new KeyNotFoundException($"Payment {request.PaymentId} not found");
            }

            // Authorization: User can only view their own payments, Admin can view all
            if (!request.IsAdmin && payment.UserId != request.UserId)
            {
                throw new UnauthorizedAccessException("You can only view your own payments");
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
