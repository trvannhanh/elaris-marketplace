using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Application.Inventory.Commands.ConfirmStockDeduction;
using System.Threading;

namespace Services.InventoryService.Infrastructure.Consumers
{
    public class ConfirmInventoryConsumer : IConsumer<ConfirmInventoryReservationCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly IInventoryService _service;
        private readonly IPublishEndpoint _publisher;
        private readonly ILogger<ConfirmInventoryConsumer> _logger;

        public ConfirmInventoryConsumer(
            IUnitOfWork uow,
            IInventoryService service,
            IPublishEndpoint publisher,
            ILogger<ConfirmInventoryConsumer> logger)
        {
            _uow = uow;
            _service = service;
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
                    await _service.ConfirmReservationAsync(cmd.OrderId, item.ProductId, item.Quantity, context.CancellationToken);
                    _logger.LogInformation("====== ConfirmReservation stock: {ProductId} x {Quantity}", item.ProductId, item.Quantity);
                }

                await _uow.SaveChangesAsync(context.CancellationToken);

                await context.Publish(new InventoryUpdatedEvent(
                    cmd.OrderId,
                    cmd.Items.Select(i => new OrderItemEntry(i.ProductId, i.Quantity)).ToList(),
                    DateTime.UtcNow
                ));

                _logger.LogInformation("✅ Inventory confirmed for Order {OrderId}", cmd.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to confirm inventory for Order {OrderId}", cmd.OrderId);

                await context.Publish(new InventoryUpdateFailedEvent(
                    cmd.OrderId,
                    ex.Message,
                    DateTime.UtcNow
                ));

                throw;
            }
        }
    }
}
