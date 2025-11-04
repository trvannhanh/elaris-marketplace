using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Orders.Commands.ChangeStatus;

namespace Services.OrderService.Infrastructure.Consumers
{
    public class PaymentFailedConsumer : IConsumer<PaymentFailedEvent>
    {
        private readonly ILogger<PaymentFailedConsumer> _logger;
        private readonly IMediator _mediator;

        public PaymentFailedConsumer(ILogger<PaymentFailedConsumer> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
        {
            var ev = context.Message;
            _logger.LogInformation("Payment failed received: Order {OrderId}", ev.OrderId);

            // Gọi use case change order status
            var cmd = new ChangeOrderStatusCommand(
                ev.OrderId,
                "Failed"
            );

            await _mediator.Send(cmd);
        }
    }
}
