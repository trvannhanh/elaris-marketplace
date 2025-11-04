
using BuildingBlocks.Contracts.Events;
using MediatR;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Commands.CreateOrder
{
    public record CreateOrderCommand(string UserId,
        List<BasketItemEvent> Items,
        decimal TotalPrice) 
    : IRequest<Order>;
}
