using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Domain.Entities;


namespace Services.InventoryService.Infrastructure.Services
{

    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<InventoryService> _logger;
        public InventoryService(IUnitOfWork uow, ILogger<InventoryService> logger)
        {
            _uow = uow;
            _logger = logger;
        }


        public async Task<InventoryItem> ConfirmReservationAsync(Guid orderId, string productId, int quantity, CancellationToken ct)
        {
            var item = await _uow.Inventory.GetByProductIdAsync(productId, ct);

            if (item == null)
            {
                throw new KeyNotFoundException($"Product {productId} not found in inventory");
            }

            // Deduct from both total and reserved
            item.Quantity -= quantity;
            item.ReservedQuantity = Math.Max(0, item.ReservedQuantity - quantity);
            item.AvailableQuantity = item.Quantity - item.ReservedQuantity;
            item.UpdatedAt = DateTime.UtcNow;
            item.Status = DetermineStatus(item.Quantity, item.LowStockThreshold);

            await _uow.Inventory.UpdateAsync(item, ct);

            await _uow.Inventory.UpdateReservationStatusAsync(orderId, ReservationStatus.Confirmed, ct);

            // Record history
            await _uow.Inventory.AddHistoryAsync(new InventoryHistory
            {
                ProductId = productId,
                ChangeType = "OrderDeduction",
                QuantityBefore = item.Quantity + quantity,
                QuantityAfter = item.Quantity,
                QuantityChanged = -quantity,
                OrderId = orderId,
                Note = $"Order {orderId} completed",
                CreatedAt = DateTime.UtcNow
            }, ct);

            _logger.LogInformation(
                "[ConfirmDeduction] Confirmed deduction of {Quantity} units of {ProductId} for order {OrderId}",
                quantity, productId, orderId);


            return item;
        }

        public async Task<InventoryItem> ReserveStockAsync(Guid orderId, string productId, int quantity, CancellationToken ct)
        {
            var item = await _uow.Inventory.GetByProductIdAsync(productId, ct);

            if (item == null)
            {
                throw new KeyNotFoundException($"Product {productId} not found in inventory");
            }

            // Check available quantity
            if (item.AvailableQuantity < quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock to reserve. Available: {item.AvailableQuantity}, Requested: {quantity}");
            }

            // Reserve stock
            item.ReservedQuantity += quantity;
            item.AvailableQuantity = item.Quantity - item.ReservedQuantity;
            item.UpdatedAt = DateTime.UtcNow;

            await _uow.Inventory.UpdateAsync(item, ct);

            // Record reservation
            await _uow.Inventory.AddReservationAsync(new StockReservation
            {
                ProductId = productId,
                OrderId = orderId,
                Quantity = quantity,
                ReservedAt = DateTime.UtcNow,
                Status = ReservationStatus.Active
            }, ct);


            _logger.LogInformation(
                "[ReserveStock] Reserved {Quantity} units of {ProductId} for order {OrderId}. Available: {Available}",
                quantity, productId, orderId, item.AvailableQuantity);

            return item;
        }

        public async Task<InventoryItem> ReleaseStockAsync(Guid orderId, string productId, int quantity, CancellationToken ct)
        {
            var item = await _uow.Inventory.GetByProductIdAsync(productId, ct);

            if (item == null)
            {
                throw new KeyNotFoundException($"Product {productId} not found in inventory");
            }

            // Release reservation
            item.ReservedQuantity = Math.Max(0, item.ReservedQuantity - quantity);
            item.AvailableQuantity = item.Quantity - item.ReservedQuantity;
            item.UpdatedAt = DateTime.UtcNow;

            await _uow.Inventory.UpdateAsync(item, ct);

            // Update reservation status
            await _uow.Inventory.UpdateReservationStatusAsync(
                orderId,
                ReservationStatus.Released,
                ct);


            _logger.LogInformation(
                "[ReleaseStock] Released {Quantity} units of {ProductId} for order {OrderId}. Available: {Available}",
                quantity, productId, orderId, item.AvailableQuantity);

            return item;
        }


        private static InventoryStatus DetermineStatus(int quantity, int threshold)
        {
            if (quantity == 0) return InventoryStatus.OutOfStock;
            if (quantity <= threshold) return InventoryStatus.LowStock;
            return InventoryStatus.InStock;
        }
    }
}
