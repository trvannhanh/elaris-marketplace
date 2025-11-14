using BuildingBlocks.Contracts.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Infrastructure.Consumers
{
    public class CompleteOrderConsumer : IConsumer<CompleteOrderCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CompleteOrderConsumer> _logger;

        public CompleteOrderConsumer(IUnitOfWork uow, ILogger<CompleteOrderConsumer> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CompleteOrderCommand> context)
        {
            var order = await _uow.Order.GetByIdAsync(context.Message.OrderId, context.CancellationToken);
            if (order == null)
            {
                _logger.LogWarning("❌ Order {OrderId} not found for completion", context.Message.OrderId);
                return;
            }

            order.Status = OrderStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("✅ Order {OrderId} completed successfully", context.Message.OrderId);
        }
    }
}
