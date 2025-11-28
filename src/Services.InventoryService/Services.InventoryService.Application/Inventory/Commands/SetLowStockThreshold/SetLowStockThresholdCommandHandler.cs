using MediatR;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.DTOs;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Application.Inventory.Commands.SetLowStockThreshold
{
    public class SetLowStockThresholdCommandHandler
    : IRequestHandler<SetLowStockThresholdCommand, InventoryItemDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SetLowStockThresholdCommandHandler> _logger;

        public SetLowStockThresholdCommandHandler(
            IUnitOfWork uow,
            ILogger<SetLowStockThresholdCommandHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<InventoryItemDto> Handle(
            SetLowStockThresholdCommand request,
            CancellationToken cancellationToken)
        {
            var item = await _uow.Inventory.GetByProductIdAsync(request.ProductId, cancellationToken);

            if (item == null)
            {
                throw new KeyNotFoundException($"Product {request.ProductId} not found in inventory");
            }

            // Authorization check
            if (request.UserRole != "admin" && item.SellerId != request.UserId)
            {
                throw new UnauthorizedAccessException("You can only update inventory for your own products");
            }

            item.LowStockThreshold = request.Threshold;
            item.UpdatedAt = DateTime.UtcNow;
            item.Status = DetermineStatus(item.Quantity, request.Threshold);

            await _uow.Inventory.UpdateAsync(item, cancellationToken);

            _logger.LogInformation(
                "[SetThreshold] User {UserId} set low stock threshold for {ProductId} to {Threshold}",
                request.UserId, request.ProductId, request.Threshold);

            await _uow.SaveChangesAsync(cancellationToken);

            return MapToDto(item);
        }

        private InventoryStatus DetermineStatus(int quantity, int threshold)
        {
            if (quantity == 0) return InventoryStatus.OutOfStock;
            if (quantity <= threshold) return InventoryStatus.LowStock;
            return InventoryStatus.InStock;
        }

        private InventoryItemDto MapToDto(InventoryItem item)
        {
            return new InventoryItemDto
            {
                ProductId = item.ProductId,
                SellerId = item.SellerId,
                Quantity = item.Quantity,
                ReservedQuantity = item.ReservedQuantity,
                AvailableQuantity = item.AvailableQuantity,
                LowStockThreshold = item.LowStockThreshold,
                Status = item.Status.ToString(),
                LastRestockDate = item.LastRestockDate,
                UpdatedAt = item.UpdatedAt
            };
        }
    }
}
