
using MediatR;
using Services.OrderService.Application.Common.Models;
using Services.OrderService.Application.Orders.DTOs;

namespace Services.OrderService.Application.Orders.GetOrdersWithFilters
{
    public record GetOrdersWithFiltersQuery(
        string? Search,
        string? UserId,
        string? SortBy,
        string? SortDirection,
        int Page = 1,
        int PageSize = 10
    ) : IRequest<PaginatedList<OrderDto>>;
}
