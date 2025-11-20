

using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Orders.Commands.CreateOrder;

namespace Services.OrderService.Infrastructure.Consumers
{
    public class BasketCheckedOutConsumer : IConsumer<BasketCheckedOutEvent>
    {
        private readonly ILogger<BasketCheckedOutConsumer> _logger;
        private readonly IMediator _mediator;

        public BasketCheckedOutConsumer(ILogger<BasketCheckedOutConsumer> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task Consume(ConsumeContext<BasketCheckedOutEvent> context)
        {
            var ev = context.Message;
            var total = ev.Items.Sum(i => i.Price * i.Quantity);
            var cardToken = "fake card token";

            _logger.LogInformation("==== Checkout received - Sending OrderCreatedCommand for User {UserId}", ev.UserId);

            await _mediator.Send(new CreateOrderCommand(
                ev.UserId,
                ev.Items,
                total,
                cardToken
            ));

            _logger.LogInformation("✅ Sent OrderCreatedCommand for User {UserId}", ev.UserId);
        }
    }
}
