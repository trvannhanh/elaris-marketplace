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
        private readonly IPaymentRepository _repo;
        private readonly IPublishEndpoint _publisher;

        public AuthorizePaymentConsumer(IPaymentRepository repo, IPublishEndpoint publisher)
        {
            _repo = repo;
            _publisher = publisher;

        }
        public async Task Consume(ConsumeContext<AuthorizePaymentCommand> context)
        {
            var cmd = context.Message;
            var payment = new Payment { OrderId = cmd.OrderId, Amount = cmd.Amount, Status = PaymentStatus.Pending };
            await _repo.AddAsync(payment, context.CancellationToken);

            await Task.Delay(1000);
            var success = new Random().NextDouble() > 0.2;

            if (success)
            {
                payment.Status = PaymentStatus.Success;
                payment.CompletedAt = DateTime.UtcNow;
                await _repo.SaveChangesAsync(context.CancellationToken);
                await context.Publish(new PaymentSucceededEvent(cmd.OrderId, cmd.Amount, new(), payment.CompletedAt.Value));
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.CompletedAt = DateTime.UtcNow;
                await _repo.SaveChangesAsync(context.CancellationToken);
                await context.Publish(new PaymentFailedEvent(cmd.OrderId, cmd.Amount, "Gateway error", payment.CompletedAt.Value));
            }
        }
    }
}
