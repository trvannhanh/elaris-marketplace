using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Infrastructure.BackgroundServices;

namespace Services.InventoryService.Infrastructure.Consumers
{
    public class ReserveInventoryConsumer : IConsumer<ReserveInventoryCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPublishEndpoint _publisher;
        private readonly ReservationTimeoutService _timeoutService;
        private readonly ILogger<ReserveInventoryConsumer> _logger;

        public ReserveInventoryConsumer(IUnitOfWork uow, IPublishEndpoint publisher, ReservationTimeoutService timeoutService, ILogger<ReserveInventoryConsumer> logger)
        {
            _uow = uow;
            _publisher = publisher;
            _timeoutService = timeoutService;
            _logger = logger;

        }
        public async Task Consume(ConsumeContext<ReserveInventoryCommand> context)
        {
            var cmd = context.Message;
            var ct = context.CancellationToken;
            var reservedItems = new List<OrderItemEntry>();

            foreach (var item in cmd.Items)
            {
                var success = await _uow.Inventory.TryReserveStockAsync(item.ProductId, item.Quantity, ct);
                if (!success)
                {
                    await context.Publish(new OrderStockRejectedEvent(
                        cmd.OrderId,
                        $"Out of stock: {item.ProductId}",
                        DateTime.UtcNow), ct);

                    _logger.LogWarning("Stock rejected for Order {OrderId}: {ProductId}", cmd.OrderId, item.ProductId);
                    return;
                }

                // THÊM VÀO QUEUE ĐỂ TỰ ĐỘNG HẾT HẠN SAU 5 PHÚT
                _timeoutService.AddReservation(cmd.OrderId, item.ProductId, item.Quantity, TimeSpan.FromMinutes(5));

                reservedItems.Add(new OrderItemEntry(item.ProductId, item.Quantity));
            }

            await _uow.SaveChangesAsync(ct);

            await context.Publish(new OrderItemsReservedEvent(
                cmd.OrderId, reservedItems, DateTime.UtcNow), context.CancellationToken);

            _logger.LogInformation("Reserved inventory for Order {OrderId}", cmd.OrderId);
        }
    }
}
