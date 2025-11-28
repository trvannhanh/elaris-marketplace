using MediatR;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Models;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Queries.GetUserPayments
{
    public record GetUserPaymentsQuery(
        string UserId,
        PaymentStatus? Status,
        int Page,
        int PageSize
    ) : IRequest<PaginatedList<PaymentDto>>;
}
