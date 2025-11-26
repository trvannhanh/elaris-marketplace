
using MediatR;
using Services.OrderService.Application.Common.Models;
using Services.OrderService.Application.Orders.DTOs;


namespace Services.OrderService.Application.Orders.Queries.GetMyOrders
{
    /// <summary>
    /// Query lấy orders của user hiện tại
    /// </summary>
    public record GetMyOrdersQuery(
        string UserId,
        int Page = 1,
        int PageSize = 10
    ) : IRequest<PaginatedList<OrderDto>>;

}
