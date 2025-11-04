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

            var order = await _inventoryRepo.FetchOrderDetails(message.OrderId);
            if (order == null)
                return; // handle later: dead-letter / retry

            bool allInStock = true;
            List<OrderItemEntry> items = new();

            foreach (var item in order.Items)
            {
                var hasStock = await _inventoryRepo.HasStockAsync(item.ProductId, item.Quantity);
                if (!hasStock)
                {
                    allInStock = false;
                    break;
                }

                items.Add(new OrderItemEntry(item.ProductId, item.Quantity));
            }

            if (!allInStock)
            {
                await _publisher.Publish(new OrderStockRejectedEvent(
                    message.OrderId,
                    "One or more items out of stock",
                    DateTime.UtcNow
                ));
                return;
            }

            //All in stock
            await _publisher.Publish(new OrderItemsReservedEvent(
                message.OrderId,
                items,
                DateTime.UtcNow
            ));
        }
    }
}
