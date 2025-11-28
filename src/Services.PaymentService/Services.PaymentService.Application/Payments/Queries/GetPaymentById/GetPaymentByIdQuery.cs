

using MediatR;
using Services.PaymentService.Application.DTOs;

namespace Services.PaymentService.Application.Payments.Queries.GetPaymentById
{
    public record GetPaymentByIdQuery(
        Guid PaymentId,
        string UserId,
        bool IsAdmin
    ) : IRequest<PaymentDto>;
}
