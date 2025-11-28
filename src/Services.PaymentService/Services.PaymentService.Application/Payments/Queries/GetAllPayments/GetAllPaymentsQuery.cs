using MediatR;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Models;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Queries.GetAllPayments
{
    public record GetAllPaymentsQuery(
        string? Search,
        string? UserId,
        PaymentStatus? Status,
        DateTime? FromDate,
        DateTime? ToDate,
        int Page,
        int PageSize
    ) : IRequest<PaginatedList<PaymentDto>>;
}
