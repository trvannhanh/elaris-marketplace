using MediatR;
using Services.OrderService.Application.Orders.DTOs;

namespace Services.OrderService.Application.Orders.Queries.GetOrderStatistics
{
    /// <summary>
    /// Query lấy thống kê orders (Admin only)
    /// </summary>
    public record GetOrderStatisticsQuery(
        DateTime? FromDate,
        DateTime? ToDate
    ) : IRequest<OrderStatisticsDto>;
}
