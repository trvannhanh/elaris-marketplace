using BuildingBlocks.Contracts.DTOs;
using MediatR;

namespace Services.OrderService.Application.Orders.Queries.GetOrderById
{
    public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto?>;
}
