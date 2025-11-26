
using MediatR;
using Services.OrderService.Application.Orders.DTOs;

namespace Services.OrderService.Application.Orders.Queries.GetOrderById
{
    public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto?>;
}
