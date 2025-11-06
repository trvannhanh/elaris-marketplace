using BuildingBlocks.Contracts.DTOs;
using MediatR;
using Services.OrderService.Application.Common.Models;

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
