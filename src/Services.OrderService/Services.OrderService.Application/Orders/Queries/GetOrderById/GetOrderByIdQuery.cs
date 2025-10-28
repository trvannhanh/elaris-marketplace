using MediatR;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Queries.GetOrderById
{
    public record GetOrderByIdQuery(Guid Id) : IRequest<Order?>;
}
