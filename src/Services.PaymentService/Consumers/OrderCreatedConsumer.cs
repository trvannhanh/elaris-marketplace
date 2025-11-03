using BuildingBlocks.Contracts.Events;
using MassTransit;

namespace Services.PaymentService.Consumers
{
    public class OrderCreatedConsumer : IConsumer<OrderEvent>
    {
        private readonly ILogger<OrderCreatedConsumer> _logger;

        public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<OrderEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("PaymentService nhận OrderCreatedEvent: OrderId={OrderId}, Total={TotalPrice}", msg.OrderId, msg.TotalPrice);

            // Giả lập xử lý thanh toán
            _logger.LogInformation("Thanh toán thành công cho đơn hàng {OrderId}", msg.OrderId);

            return Task.CompletedTask;
        }
    }
}
