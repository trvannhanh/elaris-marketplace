using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.PaymentService.Infrastructure.Consumers
{
    public class VoidPaymentConsumer : IConsumer<VoidPaymentCommand>
    {
        private readonly ILogger<VoidPaymentConsumer> _logger;

        public VoidPaymentConsumer(ILogger<VoidPaymentConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<VoidPaymentCommand> context)
        {
            var cmd = context.Message;
            var ct = context.CancellationToken;
            _logger.LogWarning(
                "VOID payment for Order {OrderId}, Amount: {Amount}, Reason: {Reason}",
                cmd.OrderId, cmd.Amount, cmd.Reason
            );

            try
            {
                // 1. Gọi gRPC/HTTP tới banking/gateway để void
                // Giả lập xử lý void
                await Task.Delay(200);

                // 2. Publish event PaymentVoidedEvent
                await context.Publish(new PaymentVoidedEvent(
                    cmd.OrderId,
                    cmd.Reason,
                    DateTime.UtcNow
                ));

                _logger.LogInformation("VOIDED payment for Order {OrderId}", cmd.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to VOID payment for Order {OrderId}", cmd.OrderId);

                await context.Publish(new PaymentVoidFailedEvent(
                    cmd.OrderId,
                    ex.Message,
                    DateTime.UtcNow
                ));

                throw;
            }
            
        }
    }
}
