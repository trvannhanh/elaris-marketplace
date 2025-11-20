using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Infrastructure.Consumers
{
    public class AuthorizePaymentConsumer : IConsumer<AuthorizePaymentCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPublishEndpoint _publisher;
        private readonly ILogger<AuthorizePaymentConsumer> _logger;

        public AuthorizePaymentConsumer(IUnitOfWork uow, IPublishEndpoint publisher, ILogger<AuthorizePaymentConsumer> logger)
        {
            _uow = uow;
            _publisher = publisher;
            _logger = logger;
        }
        public async Task Consume(ConsumeContext<AuthorizePaymentCommand> context)
        {
            var cmd = context.Message;
            var payment = new Payment { OrderId = cmd.OrderId, Amount = cmd.Amount, Status = PaymentStatus.Pending };
            _logger.LogInformation("✅ Payment created for Order {OrderId}, Amount {Amount}", cmd.OrderId, cmd.Amount);
            await _uow.Payment.AddAsync(payment, context.CancellationToken);

            await Task.Delay(1000);
            var authorized = new Random().NextDouble() > 0.2;

            if (authorized)
            {
                payment.Status = PaymentStatus.Authorized;
                payment.CompletedAt = DateTime.UtcNow;
                await _uow.SaveChangesAsync(context.CancellationToken);
                await context.Publish(new PaymentAuthorizedEvent(cmd.OrderId, cmd.Amount, payment.CompletedAt.Value));
                _logger.LogInformation("✅ Payment Authorized for Order {OrderId}: {Status}", cmd.OrderId, payment.Status);
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.CompletedAt = DateTime.UtcNow;
                await _uow.SaveChangesAsync(context.CancellationToken);
                await context.Publish(new PaymentAuthorizeFailedEvent(cmd.OrderId, cmd.Amount, "Gateway error", payment.CompletedAt.Value));
                _logger.LogWarning("❌  Payment Authoriz failed for Order {OrderId}: {Status}", cmd.OrderId, payment.Status);
            }
        }
    }
}
