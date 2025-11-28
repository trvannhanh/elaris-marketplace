using MediatR;
using Services.PaymentService.Application.DTOs;

namespace Services.PaymentService.Application.Payments.Commands.CapturePayment
{
    public record CapturePaymentCommand(
        Guid PaymentId,
        decimal Amount,
        string CapturedBy
    ) : IRequest<PaymentDto>;
}
