using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Orders.Commands.ChangeStatus;

namespace Services.OrderService.Infrastructure.Consumers
{
    public class InventoryFailedConsumer : IConsumer<InventoryFailedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<InventoryFailedConsumer> _logger;

        public InventoryFailedConsumer(IMediator mediator, ILogger<InventoryFailedConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<InventoryFailedEvent> context)
        {
            var msg = context.Message;
            _logger.LogError("❌ Inventory failed for Order {OrderId}: {Reason}",
                msg.OrderId, msg.Reason);

            await _mediator.Send(new ChangeOrderStatusCommand(
                msg.OrderId,
                "Cancelled"
            ));
        }
    }
}
