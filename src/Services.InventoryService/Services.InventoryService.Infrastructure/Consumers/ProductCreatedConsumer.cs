using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Infrastructure.Consumers
{
    public class ProductCreatedConsumer : IConsumer<ProductCreatedEvent>
    {
        private readonly IInventoryRepository _repo;
        private readonly ILogger<ProductCreatedConsumer> _logger;

        public ProductCreatedConsumer(IInventoryRepository repo, ILogger<ProductCreatedConsumer> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
        {
            var message = context.Message;
            var cancellationToken = context.CancellationToken; // lấy token ở đây

            _logger.LogInformation("📦 Received ProductCreatedEvent for ProductId={Id}", message.ProductId);

            var inventory = new InventoryItem
            {
                ProductId = message.ProductId,
                AvailableStock = 10,
                LastUpdated = DateTime.UtcNow
            };

            await _repo.AddAsync(inventory, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Inventory created for ProductId={Id}", message.ProductId);
        }
    }
}
