using MediatR;
using Microsoft.EntityFrameworkCore;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Application.Orders.DTOs;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Queries.GetOrderStatistics
{
    public class GetOrderStatisticsQueryHandler : IRequestHandler<GetOrderStatisticsQuery, OrderStatisticsDto>
    {
        private readonly IOrderRepository _repo;

        public GetOrderStatisticsQueryHandler(IOrderRepository repo)
        {
            _repo = repo;
        }

        public async Task<OrderStatisticsDto> Handle(GetOrderStatisticsQuery request, CancellationToken ct)
        {
            var query = _repo.Query();

            // Filter by date range
            if (request.FromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(o => o.CreatedAt <= request.ToDate.Value);

            var orders = await query.ToListAsync(ct);

            return new OrderStatisticsDto
            {
                TotalOrders = orders.Count,
                PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
                ProcessingOrders = orders.Count(o => o.Status == OrderStatus.Processing),
                CompletedOrders = orders.Count(o => o.Status == OrderStatus.Completed),
                FailedOrders = orders.Count(o => o.Status == OrderStatus.Failed),
                CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
                TotalRevenue = orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalPrice),
                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalPrice) : 0
            };
        }
    }
}
