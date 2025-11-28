using MediatR;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.DTOs;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Domain.Entities;


namespace Services.InventoryService.Application.Inventory.Commands.IncreaseStock
{
    public class IncreaseStockCommandHandler : IRequestHandler<IncreaseStockCommand, InventoryItemDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly ICatalogServiceClient _catalogClient;
        private readonly ILogger<IncreaseStockCommandHandler> _logger;

        public IncreaseStockCommandHandler(
            IUnitOfWork uow,
            ICatalogServiceClient catalogClient,
            ILogger<IncreaseStockCommandHandler> logger)
        {
            _uow = uow;
            _catalogClient = catalogClient;
            _logger = logger;
        }

        public async Task<InventoryItemDto> Handle(
            IncreaseStockCommand request,
            CancellationToken cancellationToken)
        {
            var item = await _uow.Inventory.GetByProductIdAsync(request.ProductId, cancellationToken);

            if (item == null)
            {
                throw new KeyNotFoundException($"Product {request.ProductId} not found in inventory");
            }

            // Authorization check: Seller can only update their own products
            if (request.UserRole != "admin" && item.SellerId != request.UserId)
            {
                throw new UnauthorizedAccessException("You can only update inventory for your own products");
            }

            // Update quantities
            var oldQuantity = item.Quantity;
            item.Quantity += request.Quantity;
            item.AvailableQuantity = item.Quantity - item.ReservedQuantity;
            item.LastRestockDate = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            item.Status = DetermineStatus(item.Quantity, item.LowStockThreshold);

            await _uow.Inventory.UpdateAsync(item, cancellationToken);

            // Record history
            await _uow.Inventory.AddHistoryAsync(new InventoryHistory
            {
                ProductId = request.ProductId,
                ChangeType = "Increase",
                QuantityBefore = oldQuantity,
                QuantityAfter = item.Quantity,
                QuantityChanged = request.Quantity,
                ChangedBy = request.UserId,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation(
                "[IncreaseStock] User {UserId} increased stock for {ProductId} by {Quantity}. New quantity: {NewQuantity}",
                request.UserId, request.ProductId, request.Quantity, item.Quantity);

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
