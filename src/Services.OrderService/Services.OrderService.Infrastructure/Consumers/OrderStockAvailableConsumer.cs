using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Orders.Commands.ChangeStatus;


namespace Services.OrderService.Infrastructure.Consumers
{
    public class OrderStockAvailableConsumer : IConsumer<OrderStockAvailableEvent>
    {
        private readonly ILogger<OrderStockAvailableConsumer> _logger;
        private readonly IMediator _mediator;

        public OrderStockAvailableConsumer(ILogger<OrderStockAvailableConsumer> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task Consume(ConsumeContext<OrderStockAvailableEvent> context)
        {
            await _mediator.Send(new ChangeOrderStatusCommand(context.Message.OrderId, "Processing"));
        }
    }
}
