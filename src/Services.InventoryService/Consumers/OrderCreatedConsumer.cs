using BuildingBlocks.Contracts.Events;
using MassTransit;

namespace Services.InventoryService.Consumers
{
    public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
    {
        private readonly ILogger<OrderCreatedConsumer> _logger;

        public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            var msg = context.Message;

            _logger.LogInformation("InventoryService nhận OrderCreatedEvent: ProductId={ProductId}", msg.ProductId);

            // Giả lập giảm số lượng tồn kho
            _logger.LogInformation("Giảm tồn kho sản phẩm {ProductId} sau khi tạo đơn hàng", msg.ProductId);

            return Task.CompletedTask;
        }
    }
}
