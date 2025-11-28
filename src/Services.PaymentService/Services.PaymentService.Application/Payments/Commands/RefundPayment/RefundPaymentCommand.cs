using MediatR;
using Services.PaymentService.Application.DTOs;

namespace Services.PaymentService.Application.Payments.Commands.RefundPayment
{
    public record RefundPaymentCommand(
        Guid PaymentId,
        decimal Amount,
        string Reason,
        string RefundedBy
    ) : IRequest<PaymentDto>;
}
