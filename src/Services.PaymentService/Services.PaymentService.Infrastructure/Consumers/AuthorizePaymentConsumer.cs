using BuildingBlocks.Contracts.Commands;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.Payments.Commands.CreatePayment;
using Services.PaymentService.Application.Payments.Commands.ProcessPayment;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Infrastructure.Consumers
{
    public class AuthorizePaymentConsumer : IConsumer<AuthorizePaymentCommand>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AuthorizePaymentConsumer> _logger;

        public AuthorizePaymentConsumer(
            IMediator mediator,
            ILogger<AuthorizePaymentConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<AuthorizePaymentCommand> context)
        {
            var cmd = context.Message;

            _logger.LogInformation(
                "[AuthorizePayment] Starting authorization for Order {OrderId}, Amount {Amount}",
                cmd.OrderId, cmd.Amount);

            try
            {
                // 1. Create Payment using CQRS Command
                var createCommand = new CreatePaymentCommand(
                    OrderId: cmd.OrderId,
                    UserId: cmd.UserId ?? "system",
                    Amount: cmd.Amount,
                    Details: null
                );

                var paymentDto = await _mediator.Send(createCommand, context.CancellationToken);

                _logger.LogInformation(
                    "[AuthorizePayment] Payment {PaymentId} created for Order {OrderId}",
                    paymentDto.Id, cmd.OrderId);

                // 2. Process Payment (Authorization) using CQRS Command
                var processCommand = new ProcessPaymentCommand(
                    PaymentId: paymentDto.Id,
                    AdditionalDetails: null
                );

                var processedPayment = await _mediator.Send(processCommand, context.CancellationToken);

                // 3. Publish appropriate event based on result
                if (processedPayment.Status == PaymentStatus.Completed.ToString())
                {
                    await context.Publish(new PaymentAuthorizedEvent(
                        cmd.OrderId,
                        cmd.Amount,
                        processedPayment.ProcessedAt ?? DateTime.UtcNow
                    ));

                    _logger.LogInformation(
                        "✅ Payment authorized for Order {OrderId}, PaymentId {PaymentId}",
                        cmd.OrderId, paymentDto.Id);
                }
                else if (processedPayment.Status == PaymentStatus.Failed.ToString())
                {
                    await context.Publish(new PaymentAuthorizeFailedEvent(
                        cmd.OrderId,
                        cmd.Amount,
                        processedPayment.FailureReason ?? "Payment authorization failed",
                        DateTime.UtcNow
                    ));

                    _logger.LogWarning(
                        "❌ Payment authorization failed for Order {OrderId}: {Reason}",
                        cmd.OrderId, processedPayment.FailureReason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Error authorizing payment for Order {OrderId}", cmd.OrderId);

                await context.Publish(new PaymentAuthorizeFailedEvent(
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
