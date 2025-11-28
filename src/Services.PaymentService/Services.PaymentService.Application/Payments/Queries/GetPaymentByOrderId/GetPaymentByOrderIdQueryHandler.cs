using MediatR;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Queries.GetPaymentByOrderId
{
    public class GetPaymentByOrderIdQueryHandler : IRequestHandler<GetPaymentByOrderIdQuery, PaymentDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IOrderServiceClient _orderClient;
        private readonly ILogger<GetPaymentByOrderIdQueryHandler> _logger;

        public GetPaymentByOrderIdQueryHandler(
            IUnitOfWork uow,
            IOrderServiceClient orderClient,
            ILogger<GetPaymentByOrderIdQueryHandler> logger)
        {
            _uow = uow;
            _orderClient = orderClient;
            _logger = logger;
        }

        public async Task<PaymentDto> Handle(
            GetPaymentByOrderIdQuery request,
            CancellationToken cancellationToken)
        {
            var payment = await _uow.Payment.GetByOrderIdAsync(request.OrderId, cancellationToken);

            if (payment == null)
            {
                throw new KeyNotFoundException($"Payment for order {request.OrderId} not found");
            }

            // Authorization check
            if (!request.IsAdmin)
            {
                var order = await _orderClient.GetOrderAsync(request.OrderId, cancellationToken);
                if (order == null || order.UserId != request.UserId)
                {
                    throw new UnauthorizedAccessException("You can only view payments for your own orders");
                }
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
