

using MediatR;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Commands.CreateOrder
{
    public record CreateOrderCommand(
        string UserId,
        string ProductId,
        int Quantity,
        string CardToken
    ) : IRequest<Order>;
}
