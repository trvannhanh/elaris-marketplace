using MediatR;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Commands.CreatePayment
{
    public record CreatePaymentCommand(Guid OrderId, decimal Amount)
        : IRequest<Payment>;
}
