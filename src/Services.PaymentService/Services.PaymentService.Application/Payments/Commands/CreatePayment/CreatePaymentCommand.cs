using MediatR;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Commands.CreatePayment
{
    public record CreatePaymentCommand(
        Guid OrderId,
        string UserId,
        decimal Amount,
        Dictionary<string, string>? Details = null
    ) : IRequest<PaymentDto>;
}
