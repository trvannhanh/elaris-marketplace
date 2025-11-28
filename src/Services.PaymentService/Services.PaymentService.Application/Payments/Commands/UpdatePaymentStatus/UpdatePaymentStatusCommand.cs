using MediatR;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Commands.UpdatePaymentStatus
{
    public record UpdatePaymentStatusCommand(
        Guid PaymentId,
        PaymentStatus NewStatus,
        string? Note,
        string UpdatedBy
    ) : IRequest<PaymentDto>;
}
