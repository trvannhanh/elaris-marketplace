using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Infrastructure.Consumers
{
    public class CapturePaymentConsumer : IConsumer<CapturePaymentCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPublishEndpoint _publisher;
        private readonly ILogger<CapturePaymentConsumer> _logger;

        public CapturePaymentConsumer(IUnitOfWork uow, IPublishEndpoint publisher, ILogger<CapturePaymentConsumer> logger)
        {
            _uow = uow;
            _publisher = publisher;
            _logger = logger;   
        }

        public async Task Consume(ConsumeContext<CapturePaymentCommand> context)
        {
            var cmd = context.Message;
            // Tìm payment đã được pre-authorized
            var payment = await _uow.Payment.GetByOrderIdAsync(cmd.OrderId, context.CancellationToken);
            if (payment == null)
            {
                _logger.LogWarning("Payment record not found for Order {OrderId}", cmd.OrderId);
                await context.Publish(new PaymentCaptureFailedEvent(cmd.OrderId, "Payment record not found", DateTime.UtcNow));
                return;
            }


            // Kiểm tra trạng thái phải là Authorized/Success (tạm giữ)
            if (payment.Status != PaymentStatus.Authorized) 
            {
                _logger.LogWarning("Payment for Order {OrderId} not in authorized state: {Status}", cmd.OrderId, payment.Status);
                await context.Publish(new PaymentCaptureFailedEvent(cmd.OrderId, $"Invalid payment state: {payment.Status}", DateTime.UtcNow));
                return;
            }

            try
            {
                // Gọi gateway thật ở đây. Ở môi trường dev, simulate:
                await Task.Delay(700, context.CancellationToken);
                var transactionId = Guid.NewGuid().ToString("N"); // giả transaction id

                // Thực hiện capture success → update entity
                payment.TransactionId = transactionId;
                payment.CompletedAt = DateTime.UtcNow;
                payment.Status = PaymentStatus.Captured; // already success, keep or set to Captured if you add new enum
                await _uow.SaveChangesAsync(context.CancellationToken);

                _logger.LogInformation("✅ Payment captured for Order {OrderId}, Tx={Tx}", cmd.OrderId, transactionId);
                await context.Publish(new PaymentCapturedEvent(cmd.OrderId, cmd.Amount, transactionId, DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Capture failed for Order {OrderId}", cmd.OrderId);
                await context.Publish(new PaymentCaptureFailedEvent(cmd.OrderId, ex.Message, DateTime.UtcNow));
            }
        }
    }
}
