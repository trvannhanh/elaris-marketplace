using BuildingBlocks.Contracts.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Infrastructure.Consumers
{
    public class CancelOrderConsumer : IConsumer<CancelOrderCommand>
    {
        private readonly IOrderRepository _repo;
        private readonly ILogger<CancelOrderConsumer> _logger;

        public CancelOrderConsumer(IOrderRepository repo, ILogger<CancelOrderConsumer> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CancelOrderCommand> context)
        {
            var order = await _repo.GetByIdAsync(context.Message.OrderId, context.CancellationToken);
            if (order == null)
            {
                _logger.LogWarning("❌ Order {OrderId} not found for cancellation", context.Message.OrderId);
                return;
            }

            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            order.CancellReason = context.Message.Reason;
            await _repo.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("✅ Order {OrderId} canceled: {Reason}", context.Message.OrderId, context.Message.Reason);
        }
    }
}
