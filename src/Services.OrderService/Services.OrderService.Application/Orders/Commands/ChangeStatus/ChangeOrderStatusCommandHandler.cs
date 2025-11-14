using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Services.OrderService.Application.Interfaces;

namespace Services.OrderService.Application.Orders.Commands.ChangeStatus
{
    public class ChangeOrderStatusCommandHandler
        : IRequestHandler<ChangeOrderStatusCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPublishEndpoint _publisher;

        public ChangeOrderStatusCommandHandler(IUnitOfWork uow, IPublishEndpoint publisher)
        {
            _uow = uow;
            _publisher = publisher;
        }

        public async Task<bool> Handle(ChangeOrderStatusCommand request, CancellationToken ct)
        {
            var order = await _uow.Order.GetByIdAsync(request.OrderId, ct);
            if (order is null) return false;

            switch (request.NewStatus)
            {
                case "Processing":
                    order.MarkProcessing();
                    break;
                case "Completed":
                    order.MarkCompleted();
                    break;
                case "Cancelled":
                    order.MarkCancelled();
                    break;
                case "Failed":
                    order.MarkFailed();
                    break;
                default:
                    throw new InvalidOperationException("Invalid status");
            }
            
            await _uow.SaveChangesAsync(ct);

            await _publisher.Publish(
                new OrderStatusUpdatedEvent(order.Id, order.Status.ToString(), DateTime.UtcNow),
                ct
            );

            return true;
        }
    }
}
