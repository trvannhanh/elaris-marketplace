using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Application.Models;
using Services.PaymentService.Domain.Entities;
using System.Net.Http.Json;

namespace Services.PaymentService.Infrastructure.Consumers
{
    public class OrderItemsReservedConsumer : IConsumer<OrderItemsReservedEvent>
    {
        private readonly ILogger<OrderItemsReservedConsumer> _logger;
        private readonly IPaymentRepository _repo;
        private readonly IPublishEndpoint _publisher;

        public OrderItemsReservedConsumer(ILogger<OrderItemsReservedConsumer> logger, IPaymentRepository repo, IPublishEndpoint publisher)
        {
            _logger = logger;
            _repo = repo;
            _publisher = publisher;
        }

        public async Task Consume(ConsumeContext<OrderItemsReservedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("OrderStockReservedEvent received: OrderId={OrderId}", msg.OrderId);

            // ✅ Fetch order details from OrderService
            var client = new HttpClient();
            var orderResponse = await client.GetFromJsonAsync<OrderDto>(
                $"http://orderservice:8080/api/orders/{msg.OrderId}");

            if (orderResponse == null)
            {
                _logger.LogError("Order not found {OrderId}", msg.OrderId);
                return;
            }

            // 1) create payment record (Pending)
            var payment = new Payment
            {
                OrderId = msg.OrderId,
                Amount = orderResponse.TotalPrice,
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

                    await _publisher.Publish(new PaymentSucceededEvent(
                        msg.OrderId,
                        payment.Amount,
                        msg.Items, 
                        payment.CompletedAt!.Value
                    ));
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
