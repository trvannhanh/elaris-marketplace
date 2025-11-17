using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Infrastructure.BackgroundServices;

namespace Services.InventoryService.Infrastructure.Consumers
{
    public class ReleaseInventoryConsumer : IConsumer<ReleaseInventoryCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly ReservationTimeoutService _timeoutService;
        private readonly ILogger<ReleaseInventoryConsumer> _logger;

        public ReleaseInventoryConsumer(
            IUnitOfWork uow,
            ReservationTimeoutService timeoutService,
            ILogger<ReleaseInventoryConsumer> logger)
        {
            _uow = uow;
            _timeoutService = timeoutService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ReleaseInventoryCommand> context)
        {
            var cmd = context.Message;
            var ct = context.CancellationToken;

            // XÓA KHỎI QUEUE ĐỂ TRÁNH TỰ ĐỘNG HẾT HẠN
            _timeoutService.RemoveReservationsByOrder(cmd.OrderId);

            foreach (var item in cmd.Items)
            {
                var success = await _uow.Inventory.ReleaseReservationAsync(item.ProductId, item.Quantity, ct);
                if (!success)
                {
                    _logger.LogWarning("❌ Stock Released error for {ProductId} x{Quantity} for Order {OrderId}", item.ProductId, item.Quantity, cmd.OrderId);
                    return;
                }
                _logger.LogInformation("✅ Released: {ProductId} x{Quantity} for Order {OrderId}", item.ProductId, item.Quantity, cmd.OrderId);
            }

            _logger.LogInformation("✅ Released inventory for Order {OrderId}", cmd.OrderId);
            await _uow.SaveChangesAsync(ct);
        }
    }
}
