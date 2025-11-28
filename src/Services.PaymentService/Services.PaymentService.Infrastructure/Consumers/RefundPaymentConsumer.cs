using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;


namespace Services.PaymentService.Infrastructure.Consumers
{
    public class RefundPaymentConsumer : IConsumer<RefundPaymentCommand>
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<RefundPaymentConsumer> _logger;

        public RefundPaymentConsumer(
            IMediator mediator,
            IUnitOfWork uow,
            ILogger<RefundPaymentConsumer> logger)
        {
            _mediator = mediator;
            _uow = uow;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<RefundPaymentCommand> context)
        {
            var cmd = context.Message;

            _logger.LogInformation(
                "[RefundPayment] Starting refund for Order {OrderId}, Reason: {Reason}",
                cmd.OrderId, cmd.Reason);

            try
            {
                // 1. Find payment by OrderId
                var payment = await _uow.Payment.GetByOrderIdAsync(
                    cmd.OrderId,
                    context.CancellationToken);

                if (payment == null)
                {
                    _logger.LogWarning(
                        "❌ Payment not found for refund: Order {OrderId}", cmd.OrderId);

                    await context.Publish(new PaymentRefundFailedEvent(
                        cmd.OrderId,
                        0,
                        "Payment record not found",
                        DateTime.UtcNow
                    ));
                    return;
                }

                // 2. Validate payment status
                if (payment.Status != PaymentStatus.Completed)
                {
                    _logger.LogWarning(
                        "❌ Cannot refund non-completed payment: {Status}", payment.Status);

                    await context.Publish(new PaymentRefundFailedEvent(
                        cmd.OrderId,
                        payment.Amount,
                        $"Invalid payment state: {payment.Status}. Expected: Completed",
                        DateTime.UtcNow
                    ));
                    return;
                }

                // 3. Refund using CQRS Command
                var refundCommand = new Application.Payments.Commands.RefundPayment.RefundPaymentCommand(
                    PaymentId: payment.Id,
                    Amount: cmd.Amount, // Full refund if amount not specified
                    Reason: cmd.Reason,
                    RefundedBy: "system"
                );

                var refundedPayment = await _mediator.Send(
                    refundCommand,
                    context.CancellationToken);

                // 4. Publish success event
                await context.Publish(new PaymentRefundedEvent(
                    cmd.OrderId,
                    refundedPayment.RefundedAmount ?? 0,
                    cmd.Reason,
                    refundedPayment.RefundedAt ?? DateTime.UtcNow
                ));

                _logger.LogInformation(
                    "✅ Payment refunded for Order {OrderId}. Amount: {Amount}, Status: {Status}",
                    cmd.OrderId, refundedPayment.RefundedAmount, refundedPayment.Status);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "❌ Refund validation failed for Order {OrderId}", cmd.OrderId);

                await context.Publish(new PaymentRefundFailedEvent(
                    cmd.OrderId,
                    cmd.Amount,
                    ex.Message,
                    DateTime.UtcNow
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Refund failed for Order {OrderId}", cmd.OrderId);

                await context.Publish(new PaymentRefundFailedEvent(
                    cmd.OrderId,
                    cmd.Amount,
                    ex.Message,
                    DateTime.UtcNow
                ));

                throw;
            }
        }
    }
}
