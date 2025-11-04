using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Infrastructure.Consumers
{
    public class PaymentSucceededConsumer : IConsumer<PaymentSucceededEvent>
    {
        private readonly IInventoryRepository _inventoryRepo;
        private readonly IPublishEndpoint _publisher;
        private readonly ILogger<PaymentSucceededConsumer> _logger;

        public PaymentSucceededConsumer(
            IInventoryRepository inventoryRepo,
            IPublishEndpoint publisher,
            ILogger<PaymentSucceededConsumer> logger)
        {
            _inventoryRepo = inventoryRepo;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
        {
            var msg = context.Message;

            try
            {
                await _inventoryRepo.DecreaseStockAsync(msg.ProductId, msg.Quantity);

                await _publisher.Publish(new InventoryUpdatedEvent(
                    msg.OrderId,
                    msg.ProductId,
                    msg.Quantity,
                    DateTime.UtcNow
                ));

                _logger.LogInformation("Inventory updated for Order {OrderId}", msg.OrderId);
            }
            catch (Exception ex)
            {
                await _publisher.Publish(new InventoryFailedEvent(
                    msg.OrderId,
                    msg.ProductId,
                    ex.Message,
                    DateTime.UtcNow
                ));

                _logger.LogError(ex, "Inventory update failed for Order {OrderId}", msg.OrderId);
            }
        }
    }
}
