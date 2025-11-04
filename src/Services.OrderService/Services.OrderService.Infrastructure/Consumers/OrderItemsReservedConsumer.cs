using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Orders.Commands.ChangeStatus;


namespace Services.OrderService.Infrastructure.Consumers
{
    public class OrderItemsReservedConsumer : IConsumer<OrderItemsReservedEvent>
    {
        private readonly ILogger<OrderItemsReservedConsumer> _logger;
        private readonly IMediator _mediator;

        public OrderItemsReservedConsumer(ILogger<OrderItemsReservedConsumer> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task Consume(ConsumeContext<OrderItemsReservedEvent> context)
        {
            await _mediator.Send(new ChangeOrderStatusCommand(context.Message.OrderId, "Processing"));
        }
    }
}
