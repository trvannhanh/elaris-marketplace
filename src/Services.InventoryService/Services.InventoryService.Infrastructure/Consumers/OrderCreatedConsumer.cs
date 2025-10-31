
using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Services.InventoryService.Application.Inventory.Commands.UpdateStock;

namespace Services.InventoryService.Infrastructure.Consumers
{
    public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
    {
        private readonly IMediator _mediator;

        public OrderCreatedConsumer(IMediator mediator)
            => _mediator = mediator;

        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            await _mediator.Send(new UpdateStockCommand(context.Message.ProductId, context.Message.Quantity));
        }
    }
}
