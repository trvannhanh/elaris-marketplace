using MediatR;
using Services.OrderService.Application.Orders.DTOs;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Queries.GetOrderById
{
    public record GetOrderByIdQuery(Guid Id) : IRequest<OrderResponse?>;
}
