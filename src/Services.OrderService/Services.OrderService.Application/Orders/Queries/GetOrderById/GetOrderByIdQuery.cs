using MediatR;
using Services.OrderService.Application.Models;

namespace Services.OrderService.Application.Orders.Queries.GetOrderById
{
    public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto?>;
}
