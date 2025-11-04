using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.OrderService.Application.Orders.Commands.ChangeStatus;

namespace Services.OrderService.Infrastructure.Consumers
{
    public class PaymentSucceededConsumer : IConsumer<PaymentSucceededEvent>
    {
        private readonly ILogger<PaymentSucceededConsumer> _logger;
        private readonly IMediator _mediator;

        public PaymentSucceededConsumer(ILogger<PaymentSucceededConsumer> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
        {
            var ev = context.Message;
            _logger.LogInformation("Payment succeeded received: Order {OrderId}", ev.OrderId);

            // Gọi use case change order status
            var cmd = new ChangeOrderStatusCommand(
                ev.OrderId,
                "Completed"
            );

            await _mediator.Send(cmd);
        }
    }
}
