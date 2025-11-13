using BuildingBlocks.Contracts.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Infrastructure.BackgroundServices;

namespace Services.InventoryService.Infrastructure.Consumers
{
    public class ReleaseInventoryConsumer : IConsumer<ReleaseInventoryCommand>
    {
        private readonly IInventoryRepository _repo;
        private readonly ReservationTimeoutService _timeoutService;
        private readonly ILogger<ReleaseInventoryConsumer> _logger;

        public ReleaseInventoryConsumer(
            IInventoryRepository repo,
            ReservationTimeoutService timeoutService,
            ILogger<ReleaseInventoryConsumer> logger)
        {
            _repo = repo;
            _timeoutService = timeoutService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ReleaseInventoryCommand> context)
        {
            var cmd = context.Message;

            // XÓA KHỎI QUEUE ĐỂ TRÁNH TỰ ĐỘNG HẾT HẠN
            _timeoutService.RemoveReservationsByOrder(cmd.OrderId);

            foreach (var item in cmd.Items)
            {
                await _repo.ReleaseReservationAsync(item.ProductId, item.Quantity, context.CancellationToken);
                _logger.LogInformation("Released: {ProductId} x{Quantity} for Order {OrderId}", item.ProductId, item.Quantity, cmd.OrderId);
            }
        }
    }
}
