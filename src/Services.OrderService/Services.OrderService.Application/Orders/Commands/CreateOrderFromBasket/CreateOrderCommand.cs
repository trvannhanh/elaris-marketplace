
using BuildingBlocks.Contracts.Events;
using MediatR;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Commands.CreateOrderFromBasket
{
    /// <summary>
    /// Command tạo order mới
    /// Buyer và Seller đều có thể tạo order
    /// </summary>
    public record CreateOrderFromBasketCommand(
        string UserId,                      // Set từ token, không cho client gửi
        List<BasketItemEvent> Items,
        decimal TotalPrice,
        string CardToken
    ) : IRequest<Order>;
}

