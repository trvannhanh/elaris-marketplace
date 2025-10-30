using BuildingBlocks.Contracts.Events;

namespace Services.OrderService.Application.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishOrderCreatedEvent(OrderCreatedEvent @event, CancellationToken cancellationToken);
    }
}
