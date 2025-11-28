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
        private readonly IInventoryService _service;
        private readonly ReservationTimeoutService _timeoutService;
        private readonly ILogger<ReserveInventoryConsumer> _logger;

        public ReserveInventoryConsumer(IUnitOfWork uow, IInventoryService service, IPublishEndpoint publisher, ReservationTimeoutService timeoutService, ILogger<ReserveInventoryConsumer> logger)
        {
            _uow = uow;
            _service = service;
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
                try
                {
                    await _service.ReserveStockAsync(cmd.OrderId, item.ProductId, item.Quantity, context.CancellationToken);

                    // THÊM VÀO QUEUE ĐỂ TỰ ĐỘNG HẾT HẠN SAU 5 PHÚT
                    _timeoutService.AddReservation(cmd.OrderId, item.ProductId, item.Quantity, TimeSpan.FromMinutes(5));

                    reservedItems.Add(new OrderItemEntry(item.ProductId, item.Quantity));
                }
                catch (Exception ex)
                {
                    await context.Publish(new InventoryReserveFailedEvent(
                        cmd.OrderId,
                        $"❌ Out of stock: {item.ProductId}",
                        DateTime.UtcNow), ct);

                    _logger.LogWarning("❌ Stock rejected for Order {OrderId}: {ProductId}", cmd.OrderId, item.ProductId);

                }
  
            }

            await _uow.SaveChangesAsync(ct);

            await context.Publish(new InventoryReservedEvent(
                cmd.OrderId, reservedItems, DateTime.UtcNow), context.CancellationToken);

            _logger.LogInformation("✅ Reserved inventory for Order {OrderId}", cmd.OrderId);
        }
    }
}
