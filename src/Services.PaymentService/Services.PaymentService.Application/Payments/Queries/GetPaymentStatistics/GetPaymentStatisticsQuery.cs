using MediatR;
using Services.PaymentService.Application.DTOs;


namespace Services.PaymentService.Application.Payments.Queries.GetPaymentStatistics
{
    public record GetPaymentStatisticsQuery(
        string? UserId,
        DateTime? FromDate,
        DateTime? ToDate
    ) : IRequest<PaymentStatisticsDto>;
}
