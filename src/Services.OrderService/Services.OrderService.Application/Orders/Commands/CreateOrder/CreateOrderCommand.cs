
using MediatR;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Commands.CreateOrder
{
    public record CreateOrderCommand(string ProductId, int Quantity, decimal TotalPrice) : IRequest<Order>;
}
