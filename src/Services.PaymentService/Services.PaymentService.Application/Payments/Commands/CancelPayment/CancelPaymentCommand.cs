using MediatR;
using Services.PaymentService.Application.DTOs;

namespace Services.PaymentService.Application.Payments.Commands.CancelPayment
{
    public record CancelPaymentCommand(
        Guid PaymentId,
        string? Reason,
        string CancelledBy
    ) : IRequest<PaymentDto>;
}
