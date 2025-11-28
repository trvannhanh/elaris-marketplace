using MediatR;
using Services.PaymentService.Application.DTOs;

namespace Services.PaymentService.Application.Payments.Queries.GetPaymentByOrderId
{
    public record GetPaymentByOrderIdQuery(
        Guid OrderId,
        string UserId,
        bool IsAdmin
    ) : IRequest<PaymentDto>;
}
