using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;


namespace Services.PaymentService.Infrastructure.Consumers
{
    public class RefundPaymentConsumer : IConsumer<RefundPaymentCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<RefundPaymentConsumer> _logger;

        public RefundPaymentConsumer(IUnitOfWork uow, ILogger<RefundPaymentConsumer> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<RefundPaymentCommand> context)
        {
            var cmd = context.Message;

            var payment = await _uow.Payment.GetByOrderIdAsync(cmd.OrderId, context.CancellationToken);
            if (payment == null)
            {
                _logger.LogWarning("❌ Payment not found for refund: Order {OrderId}", cmd.OrderId);
                return;
            }

            if (payment.Status != PaymentStatus.Authorized)
            {
                _logger.LogWarning("❌ Cannot refund non-authorzied payment: {Status}", payment.Status);
                return;
            }

            try
            {
                // Gọi cổng thanh toán thật ở đây (Stripe, VNPay, ...)
                // await _paymentGateway.RefundAsync(payment.TransactionId);

                // Giả lập refund thành công
                await Task.Delay(800, context.CancellationToken);

                payment.Status = PaymentStatus.Refunded;
                payment.RefundedAt = DateTime.UtcNow;
                payment.RefundReason = cmd.Reason;

                await _uow.SaveChangesAsync(context.CancellationToken);

                
                await context.Publish(new PaymentRefundedEvent(
                    cmd.OrderId,
                    payment.Amount,
                    cmd.Reason,
                    payment.RefundedAt.Value
                ));

                _logger.LogInformation(
                    "✅ Payment refunded for Order {OrderId}. Amount: {Amount}. Reason: {Reason}",
                    cmd.OrderId, payment.Amount, cmd.Reason
                );
            }
            catch (Exception ex)
            {
                await context.Publish(new PaymentRefundFailedEvent(
                    cmd.OrderId,
                    payment.Amount,
                    ex.Message,
                    DateTime.UtcNow
                ));

                _logger.LogError(ex, "❌ Refund failed for Order {OrderId}", cmd.OrderId);
            }
        }
    }
}
