

using MediatR;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Models;

namespace Services.PaymentService.Application.Payments.Queries.GetFailedPayments
{
    public record GetFailedPaymentsQuery(
        DateTime? FromDate,
        DateTime? ToDate,
        int Page,
        int PageSize
    ) : IRequest<PaginatedList<PaymentDto>>;
}
