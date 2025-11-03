using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Infrastructure.Consumers
{
    public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
    {
        private readonly ILogger<OrderCreatedConsumer> _logger;
        private readonly IPaymentRepository _repo;
        private readonly IPublishEndpoint _publisher;

        public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger, IPaymentRepository repo, IPublishEndpoint publisher)
        {
            _logger = logger;
            _repo = repo;
            _publisher = publisher;
        }

        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("OrderCreatedEvent received: OrderId={OrderId} Amount={Total}", msg.OrderId, msg.TotalPrice);

            // 1) create payment record (Pending)
            var payment = new Payment
            {
                OrderId = msg.OrderId,
                Amount = msg.TotalPrice,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(payment, context.CancellationToken);
            await _repo.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("Payment record created: PaymentId={PaymentId} for OrderId={OrderId}", payment.Id, msg.OrderId);

            // 2) simulate processing (call real gateway here)
            //    keep it short and cancellable
            try
            {
                // simulate delay / remote call
                await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);

                // simulate success/fail (for demo) - replace with real gateway call
                var rnd = new Random();
                var success = rnd.NextDouble() > 0.15; // 85% success rate (tweak)

                if (success)
                {
                    payment.Status = PaymentStatus.Success;
                    payment.CompletedAt = DateTime.UtcNow;
                    await _repo.SaveChangesAsync(context.CancellationToken);

                    _logger.LogInformation("Payment succeeded for OrderId={OrderId}", msg.OrderId);

                    await _publisher.Publish(new PaymentSucceededEvent(msg.OrderId, payment.Amount, payment.CompletedAt!.Value), context.CancellationToken);
                }
                else
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.CompletedAt = DateTime.UtcNow;
                    await _repo.SaveChangesAsync(context.CancellationToken);

                    var reason = "Simulated gateway failure";
                    _logger.LogWarning("Payment failed for OrderId={OrderId}: {Reason}", msg.OrderId, reason);

                    await _publisher.Publish(new PaymentFailedEvent(msg.OrderId, payment.Amount, reason, payment.CompletedAt!.Value), context.CancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Payment processing canceled for OrderId={OrderId}", msg.OrderId);
                // optionally mark payment as failed/canceled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during payment processing for OrderId={OrderId}", msg.OrderId);
                // mark failed & publish PaymentFailedEvent
                payment.Status = PaymentStatus.Failed;
                payment.CompletedAt = DateTime.UtcNow;
                await _repo.SaveChangesAsync(context.CancellationToken);

                await _publisher.Publish(new PaymentFailedEvent(msg.OrderId, payment.Amount, ex.Message, payment.CompletedAt!.Value), context.CancellationToken);
            }
        }
    }
}
