using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Infrastructure.Consumers
{
    public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
    {
        private readonly IInventoryRepository _inventoryRepo;
        private readonly IPublishEndpoint _publisher;

        public OrderCreatedConsumer(IInventoryRepository repo, IPublishEndpoint publisher)
        {
            _inventoryRepo = repo;
            _publisher = publisher;
        }

        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            var message = context.Message;

            //var hasStock = await _inventoryRepo.HasStockAsync(message.ProductId, message.Quantity);
            var hasStock = true;
            if (!hasStock)
            {
                await _publisher.Publish(new OrderStockRejectedEvent(
                    message.OrderId,
                    "Out of stock",
                    DateTime.UtcNow
                ));

                return;
            }

            //await _publisher.Publish(new OrderStockAvailableEvent(
            //    message.OrderId,
            //    message.ProductId,
            //    message.Quantity,
            //    DateTime.UtcNow
            //));
        }
    }
}
