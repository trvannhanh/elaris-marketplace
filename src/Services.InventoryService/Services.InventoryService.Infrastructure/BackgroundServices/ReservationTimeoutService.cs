
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;
using System.Collections.Concurrent;

namespace Services.InventoryService.Infrastructure.BackgroundServices
{
    public class ReservationTimeoutService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationTimeoutService> _logger;


        private readonly ConcurrentDictionary<Guid, ConcurrentBag<(string itemId, int quantity, DateTime expireAt)>> _reservationsByOrder = new();
        public ReservationTimeoutService(
            IServiceScopeFactory scopeFactory,
            ILogger<ReservationTimeoutService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // Gọi từ Consumer khi giữ hàng
        public void AddReservation(Guid orderId, string itemId, int quantity, TimeSpan duration)
        {
            var expireAt = DateTime.UtcNow.Add(duration);
            var bag = _reservationsByOrder.GetOrAdd(orderId, _ => new ConcurrentBag<(string, int, DateTime)>());
            bag.Add((itemId, quantity, expireAt));
        }

        public void RemoveReservationsByOrder(Guid orderId)
        {
            _reservationsByOrder.TryRemove(orderId, out _);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                foreach (var kvp in _reservationsByOrder)
                {
                    var orderId = kvp.Key;
                    var bag = kvp.Value;
                    var expired = new List<(string, int)>();

                    while (bag.TryTake(out var item))
                    {
                        if (item.expireAt <= now)
                        {
                            expired.Add((item.itemId, item.quantity));
                        }
                        else
                        {
                            bag.Add(item);
                        }
                    }

                    if (expired.Count > 0)
                    {
                        // Tạo scope mới để resolve repository
                        using var scope = _scopeFactory.CreateScope();
                        var repo = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();

                        foreach (var (itemId, quantity) in expired)
                        {
                            try
                            {
                                await repo.ReleaseReservationAsync(itemId, quantity, stoppingToken);
                                _logger.LogInformation("Auto-released: Order {OrderId} - {ProductId} x{Quantity}", orderId, itemId, quantity);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to auto-release reservation for Order {OrderId}", orderId);
                            }
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
