using BuildingBlocks.Contracts.Events;
using MassTransit;
using Services.OrderService.Application.Interfaces;

namespace Services.OrderService.Infrastructure.Publishers
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public EventPublisher(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        public Task PublishOrderCreatedEvent(OrderEvent @event, CancellationToken cancellationToken)
        {
            return _publishEndpoint.Publish(@event, cancellationToken);
        }
    }
}
