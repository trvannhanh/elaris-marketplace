using BuildingBlocks.Contracts.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;


namespace Services.PaymentService.Infrastructure.Consumers
{
    public class RefundPaymentConsumer : IConsumer<RefundPaymentCommand>
    {
        private readonly IPaymentRepository _repo;
        private readonly ILogger<RefundPaymentConsumer> _logger;

        public RefundPaymentConsumer(IPaymentRepository repo, ILogger<RefundPaymentConsumer> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<RefundPaymentCommand> context)
        {
            var cmd = context.Message;

            var payment = await _repo.GetByOrderIdAsync(cmd.OrderId, context.CancellationToken);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for refund: Order {OrderId}", cmd.OrderId);
                return;
            }

            if (payment.Status != PaymentStatus.Success)
            {
                _logger.LogWarning("Cannot refund non-successful payment: {Status}", payment.Status);
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

                await _repo.SaveChangesAsync(context.CancellationToken);

                _logger.LogInformation(
                    "Payment refunded for Order {OrderId}. Amount: {Amount}. Reason: {Reason}",
                    cmd.OrderId, payment.Amount, cmd.Reason
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refund failed for Order {OrderId}", cmd.OrderId);
                // Có thể publish RefundFailedEvent nếu cần
            }
        }
    }
}
