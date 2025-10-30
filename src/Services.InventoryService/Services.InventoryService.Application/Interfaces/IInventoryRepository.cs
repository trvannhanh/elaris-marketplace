using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Application.Interfaces
{
    public interface IInventoryRepository
    {
        Task<InventoryItem?> GetByProductIdAsync(string productId, CancellationToken ct);
        Task UpdateStockAsync(InventoryItem item, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
