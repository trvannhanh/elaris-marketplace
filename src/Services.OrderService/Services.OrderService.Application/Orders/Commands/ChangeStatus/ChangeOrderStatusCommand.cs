using MediatR;

namespace Services.OrderService.Application.Orders.Commands.ChangeStatus
{
    public record ChangeOrderStatusCommand(Guid OrderId, string NewStatus) : IRequest<bool>;
}
