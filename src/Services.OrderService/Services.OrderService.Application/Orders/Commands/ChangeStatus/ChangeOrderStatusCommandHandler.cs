using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Services.OrderService.Application.Interfaces;

namespace Services.OrderService.Application.Orders.Commands.ChangeStatus
{
    public class ChangeOrderStatusCommandHandler
        : IRequestHandler<ChangeOrderStatusCommand, bool>
    {
        private readonly IOrderRepository _repo;
        private readonly IPublishEndpoint _publisher;

        public ChangeOrderStatusCommandHandler(IOrderRepository repo, IPublishEndpoint publisher)
        {
            _repo = repo;
            _publisher = publisher;
        }

        public async Task<bool> Handle(ChangeOrderStatusCommand request, CancellationToken ct)
        {
            var order = await _repo.GetByIdAsync(request.OrderId, ct);
            if (order is null) return false;

            switch (request.NewStatus)
            {
                case "Processing":
                    order.MarkProcessing();
                    break;
                case "Completed":
                    order.MarkCompleted();
                    break;
                case "Failed":
                    order.MarkFailed();
                    break;
                default:
                    throw new InvalidOperationException("Invalid status");
            }

            await _repo.SaveChangesAsync(ct);

            await _publisher.Publish(
                new OrderStatusUpdatedEvent(order.Id, order.Status.ToString(), DateTime.UtcNow),
                ct
            );

            return true;
        }
    }
}
