using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Infrastructure.BackgroundServices
{
    public class ExpiredReservationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpiredReservationCleanupService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _reservationTimeout = TimeSpan.FromMinutes(15);

        public ExpiredReservationCleanupService(
            IServiceProvider serviceProvider,
            ILogger<ExpiredReservationCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[ExpiredReservationCleanup] Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredReservationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ExpiredReservationCleanup] Error during cleanup");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("[ExpiredReservationCleanup] Service stopped");
        }

        private async Task CleanupExpiredReservationsAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();

            var expirationTime = DateTime.UtcNow.Subtract(_reservationTimeout);
            var expiredReservations = await repository.GetExpiredReservationsAsync(expirationTime, ct);

            if (!expiredReservations.Any())
            {
                _logger.LogDebug("[ExpiredReservationCleanup] No expired reservations found");
                return;
            }

            _logger.LogInformation(
                "[ExpiredReservationCleanup] Found {Count} expired reservations",
                expiredReservations.Count);

            foreach (var reservation in expiredReservations)
            {
                try
                {
                    // Release the reserved stock
                    var item = await repository.GetByProductIdAsync(reservation.ProductId, ct);
                    if (item != null)
                    {
                        item.ReservedQuantity = Math.Max(0, item.ReservedQuantity - reservation.Quantity);
                        item.AvailableQuantity = item.Quantity - item.ReservedQuantity;
                        item.UpdatedAt = DateTime.UtcNow;

                        await repository.UpdateAsync(item, ct);
                    }

                    // Update reservation status
                    await repository.UpdateReservationStatusAsync(
                        reservation.OrderId,
                        ReservationStatus.Expired,
                        ct);

                    _logger.LogInformation(
                        "[ExpiredReservationCleanup] Released expired reservation for order {OrderId}, product {ProductId}",
                        reservation.OrderId, reservation.ProductId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[ExpiredReservationCleanup] Failed to release reservation for order {OrderId}",
                        reservation.OrderId);
                }
            }
        }
    }
}
