using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Infrastructure.Consumers
{
    public class CapturePaymentConsumer : IConsumer<CapturePaymentCommand>
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CapturePaymentConsumer> _logger;

        public CapturePaymentConsumer(
            IMediator mediator,
            IUnitOfWork uow,
            ILogger<CapturePaymentConsumer> logger)
        {
            _mediator = mediator;
            _uow = uow;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CapturePaymentCommand> context)
        {
            var cmd = context.Message;

            _logger.LogInformation(
                "[CapturePayment] Starting capture for Order {OrderId}",
                cmd.OrderId);

            try
            {
                // 1. Find payment by OrderId
                var payment = await _uow.Payment.GetByOrderIdAsync(
                    cmd.OrderId,
                    context.CancellationToken);

                if (payment == null)
                {
                    _logger.LogWarning(
                        "❌ Payment record not found for Order {OrderId}", cmd.OrderId);

                    await context.Publish(new PaymentCaptureFailedEvent(
                        cmd.OrderId,
                        "Payment record not found",
                        DateTime.UtcNow
                    ));
                    return;
                }

                // 2. Validate payment status
                if (payment.Status != PaymentStatus.Completed)
                {
                    _logger.LogWarning(
                        "❌ Payment for Order {OrderId} not in completed state: {Status}",
                        cmd.OrderId, payment.Status);

                    await context.Publish(new PaymentCaptureFailedEvent(
                        cmd.OrderId,
                        $"Invalid payment state: {payment.Status}. Expected: Completed",
                        DateTime.UtcNow
                    ));
                    return;
                }

                // 3. Capture using CQRS Command
                var captureCommand = new Application.Payments.Commands.CapturePayment.CapturePaymentCommand(
                    PaymentId: payment.Id,
                    Amount: cmd.Amount,
                    CapturedBy: "system"
                );

                var capturedPayment = await _mediator.Send(
                    captureCommand,
                    context.CancellationToken);

                // 4. Publish success event
                await context.Publish(new PaymentCapturedEvent(
                    cmd.OrderId,
                    cmd.Amount,
                    capturedPayment.TransactionId ?? string.Empty,
                    capturedPayment.CapturedAt ?? DateTime.UtcNow
                ));

                _logger.LogInformation(
                    "✅ Payment captured for Order {OrderId}, PaymentId {PaymentId}, Tx {TransactionId}",
                    cmd.OrderId, payment.Id, capturedPayment.TransactionId);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "❌ Capture validation failed for Order {OrderId}", cmd.OrderId);

                await context.Publish(new PaymentCaptureFailedEvent(
                    cmd.OrderId,
                    ex.Message,
                    DateTime.UtcNow
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Capture failed for Order {OrderId}", cmd.OrderId);

                await context.Publish(new PaymentCaptureFailedEvent(
                    cmd.OrderId,
                    ex.Message,
                    DateTime.UtcNow
                ));

                throw;
            }
        }
    }
}
