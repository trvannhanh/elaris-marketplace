using MediatR;
using Services.PaymentService.Application.DTOs;

namespace Services.PaymentService.Application.Payments.Commands.RetryPayment
{
    public record RetryPaymentCommand(
        Guid PaymentId,
        Dictionary<string, string>? AdditionalDetails = null
    ) : IRequest<PaymentDto>;
}
