using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Infrastructure.Consumers
{
    public class ProductCreatedConsumer : IConsumer<ProductCreatedEvent>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ProductCreatedConsumer> _logger;

        public ProductCreatedConsumer(IUnitOfWork uow, ILogger<ProductCreatedConsumer> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
        {
            var message = context.Message;
            var ct = context.CancellationToken;

            _logger.LogInformation("📦 Received ProductCreatedEvent for ProductId={Id}", message.ProductId);

            var inventory = new InventoryItem
            {
                ProductId = message.ProductId,
                SellerId = message.SellerId,
                Quantity = message.Quantity,
                ReservedQuantity = 0,
                AvailableQuantity = message.Quantity,
                LowStockThreshold = message.LowStockThreshold,
                Status = DetermineStatus(message.Quantity, message.LowStockThreshold),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _uow.Inventory.AddAsync(inventory, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("✅ Inventory created for ProductId={Id}", message.ProductId);
        }

        private static InventoryStatus DetermineStatus(int quantity, int threshold)
        {
            if (quantity == 0) return InventoryStatus.OutOfStock;
            if (quantity <= threshold) return InventoryStatus.LowStock;
            return InventoryStatus.InStock;
        }
    }
}
