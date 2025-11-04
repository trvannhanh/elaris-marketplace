using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Orders.Commands.ChangeStatus;

namespace Services.OrderService.Infrastructure.Consumers
{
    public class InventoryUpdatedConsumer : IConsumer<InventoryUpdatedEvent>
    {
        private readonly ILogger<InventoryUpdatedConsumer> _logger;
        private readonly IMediator _mediator;

        public InventoryUpdatedConsumer(ILogger<InventoryUpdatedConsumer> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task Consume(ConsumeContext<InventoryUpdatedEvent> context)
        {
            var ev = context.Message;
            _logger.LogInformation("Inventory updated received: Order {OrderId}", ev.OrderId);

            // Gọi use case change order status
            var cmd = new ChangeOrderStatusCommand(
                ev.OrderId,
                "Completed"
            );

            await _mediator.Send(cmd);
        }
    }
}
