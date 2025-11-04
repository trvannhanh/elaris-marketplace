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

        public PaymentSucceededConsumer(ILogger<PaymentSucceededConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<PaymentSucceededEvent> context)
        {
            var ev = context.Message;
            _logger.LogInformation("Payment succeeded for Order {OrderId}. Waiting inventory confirm...", ev.OrderId);
            return Task.CompletedTask;
        }
    }
}
