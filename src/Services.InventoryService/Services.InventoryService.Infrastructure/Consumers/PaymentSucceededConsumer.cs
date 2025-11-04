using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Infrastructure.Consumers
{
    public class PaymentSucceededConsumer : IConsumer<PaymentSucceededEvent>
    {
        private readonly IInventoryRepository _inventoryRepo;
        private readonly ILogger<PaymentSucceededConsumer> _logger;

        public PaymentSucceededConsumer(IInventoryRepository inventoryRepo, ILogger<PaymentSucceededConsumer> logger)
        {
            _inventoryRepo = inventoryRepo;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
        {
            var msg = context.Message;

            _logger.LogInformation("Payment success → Decrease stock for Order {OrderId}", msg.OrderId);

            await _inventoryRepo.DecreaseStockAsync(msg.ProductId, msg.Quantity);
        }
    }
}
