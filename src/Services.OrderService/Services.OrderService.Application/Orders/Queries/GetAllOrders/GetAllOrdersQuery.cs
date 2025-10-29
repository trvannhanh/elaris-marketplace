using MediatR;
using Services.OrderService.Application.Orders.DTOs;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Queries.GetAllOrders
{
    public record GetAllOrdersQuery : IRequest<IEnumerable<OrderResponse>>;
}
