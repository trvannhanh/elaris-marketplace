using MediatR;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.Commands.ChangeStatus
{
    /// <summary>
    /// Command update order status (Admin only hoặc internal system)
    /// </summary>
    public record UpdateOrderStatusCommand(
        Guid OrderId,
        OrderStatus NewStatus,
        string? Note
    ) : IRequest<bool>;
}
