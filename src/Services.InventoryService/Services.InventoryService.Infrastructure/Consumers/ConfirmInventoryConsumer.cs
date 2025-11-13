using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Infrastructure.Consumers
{
    public class ConfirmInventoryConsumer : IConsumer<ConfirmInventoryReservationCommand>
    {
        private readonly IInventoryRepository _repo;
        private readonly IPublishEndpoint _publisher;
        private readonly ILogger<ConfirmInventoryConsumer> _logger;

        public ConfirmInventoryConsumer(
            IInventoryRepository repo,
            IPublishEndpoint publisher,
            ILogger<ConfirmInventoryConsumer> logger)
        {
            _repo = repo;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ConfirmInventoryReservationCommand> context)
        {
            var cmd = context.Message;

            try
            {
                foreach (var item in cmd.Items)
                {
                    await _repo.ConfirmReservationAsync(item.ProductId, item.Quantity);
                    _logger.LogInformation("ConfirmReservation stock: {ProductId} x {Quantity}", item.ProductId, item.Quantity);
                }

                await context.Publish(new InventoryUpdatedEvent(
                    cmd.OrderId,
                    cmd.Items.Select(i => new OrderItemEntry(i.ProductId, i.Quantity)).ToList(),
                    DateTime.UtcNow
                ));

                _logger.LogInformation("Inventory confirmed for Order {OrderId}", cmd.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to confirm inventory for Order {OrderId}", cmd.OrderId);

                await context.Publish(new InventoryFailedEvent(
                    cmd.OrderId,
                    ex.Message,
                    DateTime.UtcNow
                ));
            }
        }
    }
}
