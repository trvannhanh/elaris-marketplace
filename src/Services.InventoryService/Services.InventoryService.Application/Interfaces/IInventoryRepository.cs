using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Application.Interfaces
{
    public interface IInventoryRepository
    {
        Task<bool> HasStockAsync(string productId, int quantity, CancellationToken cancellationToken = default);
        Task DecreaseStockAsync(string productId, int quantity, CancellationToken cancellationToken = default);

        Task<InventoryItem?> GetByProductIdAsync(string productId, CancellationToken cancellationToken = default);
        Task AddAsync(InventoryItem inventory, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
