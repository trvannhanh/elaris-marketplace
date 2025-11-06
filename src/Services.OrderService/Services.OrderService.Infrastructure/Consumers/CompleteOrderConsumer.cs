using BuildingBlocks.Contracts.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Infrastructure.Consumers
{
    public class CompleteOrderConsumer : IConsumer<CompleteOrderCommand>
    {
        private readonly IOrderRepository _repo;
        private readonly ILogger<CompleteOrderConsumer> _logger;

        public CompleteOrderConsumer(IOrderRepository repo, ILogger<CompleteOrderConsumer> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CompleteOrderCommand> context)
        {
            var order = await _repo.GetByIdAsync(context.Message.OrderId, context.CancellationToken);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for completion", context.Message.OrderId);
                return;
            }

            order.Status = OrderStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;
            await _repo.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("Order {OrderId} completed successfully", context.Message.OrderId);
        }
    }
}
