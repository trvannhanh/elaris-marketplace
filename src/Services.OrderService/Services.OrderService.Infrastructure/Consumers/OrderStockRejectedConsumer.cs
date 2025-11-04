using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Orders.Commands.ChangeStatus;

namespace Services.OrderService.Infrastructure.Consumers
{
    public class OrderStockRejectedConsumer : IConsumer<OrderStockRejectedEvent>
    {
        private readonly ILogger<OrderStockRejectedConsumer> _logger;
        private readonly IMediator _mediator;

        public OrderStockRejectedConsumer(ILogger<OrderStockRejectedConsumer> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task Consume(ConsumeContext<OrderStockRejectedEvent> context)
        {
            await _mediator.Send(new ChangeOrderStatusCommand(context.Message.OrderId, "Cancelled"));
        }
    }
}
