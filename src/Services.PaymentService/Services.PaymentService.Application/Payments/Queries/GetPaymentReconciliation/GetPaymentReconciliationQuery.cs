
using MediatR;
using Services.PaymentService.Application.DTOs;

namespace Services.PaymentService.Application.Payments.Queries.GetPaymentReconciliation
{
    public record GetPaymentReconciliationQuery(
        DateTime FromDate,
        DateTime ToDate
    ) : IRequest<PaymentReconciliationDto>;
}
