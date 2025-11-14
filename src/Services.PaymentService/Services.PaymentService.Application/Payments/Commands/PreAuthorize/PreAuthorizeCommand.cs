using MediatR;
using Services.PaymentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.PaymentService.Application.Payments.Commands.PreAuthorize
{
    public record PreAuthorizeCommand(
    Guid OrderId,
    decimal Amount,
    string UserId
) : IRequest<PreAuthorizeResult>;

    public record PreAuthorizeResult(
        bool Success,
        Guid PaymentId,
        string Message,
        PaymentStatus Status
    );
}
