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

        public AuthorizePaymentConsumer(IUnitOfWork uow, IPublishEndpoint publisher)
        {
            _uow = uow;
            _publisher = publisher;

        }
        public async Task Consume(ConsumeContext<AuthorizePaymentCommand> context)
        {
            var cmd = context.Message;
            var payment = new Payment { OrderId = cmd.OrderId, Amount = cmd.Amount, Status = PaymentStatus.Pending };
            await _uow.Payment.AddAsync(payment, context.CancellationToken);

            await Task.Delay(1000);
            var authorized = new Random().NextDouble() > 0.2;

            if (authorized)
            {
                payment.Status = PaymentStatus.Authorized;
                payment.CompletedAt = DateTime.UtcNow;
                await _uow.SaveChangesAsync(context.CancellationToken);
                await context.Publish(new PaymentSucceededEvent(cmd.OrderId, cmd.Amount, payment.CompletedAt.Value));
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.CompletedAt = DateTime.UtcNow;
                await _uow.SaveChangesAsync(context.CancellationToken);
                await context.Publish(new PaymentFailedEvent(cmd.OrderId, cmd.Amount, "Gateway error", payment.CompletedAt.Value));
            }
        }
    }
}
