using MediatR;
using Services.PaymentService.Application.DTOs;

namespace Services.PaymentService.Application.Payments.Commands.ProcessPayment
{
    public record ProcessPaymentCommand(
        Guid PaymentId,
        Dictionary<string, string>? AdditionalDetails = null
    ) : IRequest<PaymentDto>;
}
