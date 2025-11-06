using BuildingBlocks.Contracts.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Services.InventoryService.Infrastructure.Consumers
{
    public class ReleaseInventoryConsumer : IConsumer<ReleaseInventoryCommand>
    {
        private readonly ILogger<ReleaseInventoryConsumer> _logger;

        public ReleaseInventoryConsumer(ILogger<ReleaseInventoryConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<ReleaseInventoryCommand> context)
        {
            var cmd = context.Message;
            _logger.LogInformation(
                "Released inventory reservation for Order {OrderId}. Items: {Items}",
                cmd.OrderId,
                string.Join(", ", cmd.Items.Select(i => $"{i.ProductId}x{i.Quantity}"))
            );

            // Không cần làm gì: hàng chưa bị trừ → chỉ log
            return Task.CompletedTask;
        }
    }
}
