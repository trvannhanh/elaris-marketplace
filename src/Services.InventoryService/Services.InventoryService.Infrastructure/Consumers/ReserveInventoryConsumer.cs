using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Infrastructure.Consumers
{
    public class ReserveInventoryConsumer : IConsumer<ReserveInventoryCommand>
    {
        private readonly IInventoryRepository _repo;
        private readonly IPublishEndpoint _publisher;

        public ReserveInventoryConsumer(IInventoryRepository repo, IPublishEndpoint publisher)
        {
            _repo = repo;
            _publisher = publisher;
        }

        public async Task Consume(ConsumeContext<ReserveInventoryCommand> context)
        {
            var cmd = context.Message;
            var reservedItems = new List<OrderItemEntry>();

            foreach (var item in cmd.Items)
            {
                var inStock = await _repo.HasStockAsync(item.ProductId, item.Quantity);
                if (!inStock)
                {
                    await context.Publish(new OrderStockRejectedEvent(cmd.OrderId, $"Out of stock: {item.ProductId}", DateTime.UtcNow));
                    return;
                }
                reservedItems.Add(new OrderItemEntry(item.ProductId, item.Quantity));
            }

            await context.Publish(new OrderItemsReservedEvent(cmd.OrderId, reservedItems, DateTime.UtcNow));
        }
    }
}
