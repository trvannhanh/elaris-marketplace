using MediatR;
using Microsoft.Extensions.Logging;
using Services.InventoryService.Application.Interfaces;


namespace Services.InventoryService.Application.Inventory.Commands.BulkUpdateInventory
{
    public class BulkUpdateInventoryCommandHandler
    : IRequestHandler<BulkUpdateInventoryCommand, BulkUpdateResult>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<BulkUpdateInventoryCommandHandler> _logger;

        public BulkUpdateInventoryCommandHandler(
            IUnitOfWork uow,
            ILogger<BulkUpdateInventoryCommandHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<BulkUpdateResult> Handle(
            BulkUpdateInventoryCommand request,
            CancellationToken cancellationToken)
        {
            var result = new BulkUpdateResult();

            foreach (var item in request.Items)
            {
                try
                {
                    var inventoryItem = await _uow.Inventory.GetByProductIdAsync(
                        item.ProductId, cancellationToken);

                    if (inventoryItem == null)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Product {item.ProductId} not found");
                        continue;
                    }

                    inventoryItem.Quantity = item.Quantity;
                    inventoryItem.AvailableQuantity = item.Quantity - inventoryItem.ReservedQuantity;
                    inventoryItem.UpdatedAt = DateTime.UtcNow;

                    await _uow.Inventory.UpdateAsync(inventoryItem, cancellationToken);
                    result.SuccessCount++;

                    await _uow.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Product {item.ProductId}: {ex.Message}");
                    _logger.LogError(ex, "[BulkUpdate] Failed to update {ProductId}", item.ProductId);
                }
            }

            _logger.LogInformation(
                "[BulkUpdate] Completed. Success: {Success}, Failure: {Failure}",
                result.SuccessCount, result.FailureCount);

            return result;
        }
    }
}
