using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Infrastructure.BackgroundServices
{
    public class StuckPaymentCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StuckPaymentCleanupService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10);
        private readonly TimeSpan _paymentTimeout = TimeSpan.FromMinutes(30);

        public StuckPaymentCleanupService(
            IServiceProvider serviceProvider,
            ILogger<StuckPaymentCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[StuckPaymentCleanup] Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupStuckPaymentsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[StuckPaymentCleanup] Error during cleanup");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("[StuckPaymentCleanup] Service stopped");
        }

        private async Task CleanupStuckPaymentsAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var timeoutThreshold = DateTime.UtcNow.Subtract(_paymentTimeout);
            var stuckPayments = await unitOfWork.Payment.GetPendingPaymentsAsync(timeoutThreshold, ct);

            if (!stuckPayments.Any())
            {
                _logger.LogDebug("[StuckPaymentCleanup] No stuck payments found");
                return;
            }

            _logger.LogWarning(
                "[StuckPaymentCleanup] Found {Count} stuck payments",
                stuckPayments.Count);

            foreach (var payment in stuckPayments)
            {
                try
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.FailureReason = "Payment timeout - automatically cancelled";
                    payment.UpdatedAt = DateTime.UtcNow;

                    await unitOfWork.Payment.UpdateAsync(payment, ct);

                    // Add history
                    await unitOfWork.Payment.AddHistoryAsync(new PaymentHistory
                    {
                        PaymentId = payment.Id,
                        Action = "Auto-cancelled due to timeout",
                        ChangedBy = "System",
                        Note = $"Payment stuck in Pending status for over {_paymentTimeout.TotalMinutes} minutes",
                        CreatedAt = DateTime.UtcNow
                    }, ct);

                    _logger.LogInformation(
                        "[StuckPaymentCleanup] Auto-cancelled stuck payment {PaymentId} for order {OrderId}",
                        payment.Id, payment.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[StuckPaymentCleanup] Failed to cancel payment {PaymentId}",
                        payment.Id);
                }
            }
        }
    }
}
