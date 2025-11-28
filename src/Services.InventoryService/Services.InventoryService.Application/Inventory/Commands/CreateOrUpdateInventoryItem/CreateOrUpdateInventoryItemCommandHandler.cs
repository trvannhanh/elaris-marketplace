using MediatR;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.DTOs;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Application.Inventory.Commands.CreateOrUpdateInventoryItem
{
    public class CreateOrUpdateInventoryItemCommandHandler
    : IRequestHandler<CreateOrUpdateInventoryItemCommand, InventoryItemDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly ICatalogServiceClient _catalogClient;
        private readonly ILogger<CreateOrUpdateInventoryItemCommandHandler> _logger;

        public CreateOrUpdateInventoryItemCommandHandler(
            IUnitOfWork uow,
            ICatalogServiceClient catalogClient,
            ILogger<CreateOrUpdateInventoryItemCommandHandler> logger)
        {
            _uow = uow;
            _catalogClient = catalogClient;
            _logger = logger;
        }

        public async Task<InventoryItemDto> Handle(
            CreateOrUpdateInventoryItemCommand request,
            CancellationToken cancellationToken)
        {
            // Validate product exists
            var product = await _catalogClient.GetProductAsync(request.ProductId, cancellationToken);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product {request.ProductId} not found");
            }

            var existingItem = await _uow.Inventory.GetByProductIdAsync(request.ProductId, cancellationToken);

            if (existingItem != null)
            {
                // Update existing
                existingItem.Quantity = request.Quantity;
                existingItem.LowStockThreshold = request.LowStockThreshold;
                existingItem.UpdatedAt = DateTime.UtcNow;
                existingItem.Status = DetermineStatus(request.Quantity, request.LowStockThreshold);

                await _uow.Inventory.UpdateAsync(existingItem, cancellationToken);

                _logger.LogInformation(
                    "[UpdateInventory] Updated inventory for product {ProductId}. Quantity: {Quantity}",
                    request.ProductId, request.Quantity);

                await _uow.SaveChangesAsync(cancellationToken);

                return MapToDto(existingItem);
            }
            else
            {
                // Create new
                var newItem = new InventoryItem
                {
                    ProductId = request.ProductId,
                    SellerId = request.SellerId ?? product.SellerId,
                    Quantity = request.Quantity,
                    ReservedQuantity = 0,
                    AvailableQuantity = request.Quantity,
                    LowStockThreshold = request.LowStockThreshold,
                    Status = DetermineStatus(request.Quantity, request.LowStockThreshold),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _uow.Inventory.AddAsync(newItem, cancellationToken);

                _logger.LogInformation(
                    "[CreateInventory] Created inventory for product {ProductId}. Quantity: {Quantity}",
                    request.ProductId, request.Quantity);

                await _uow.SaveChangesAsync(cancellationToken);

                return MapToDto(newItem);
            }
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
