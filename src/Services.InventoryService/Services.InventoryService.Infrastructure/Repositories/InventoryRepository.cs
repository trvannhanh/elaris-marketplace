
using Microsoft.EntityFrameworkCore;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Domain.Entities;
using Services.InventoryService.Infrastructure.Persistence;

namespace Services.InventoryService.Infrastructure.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly InventoryDbContext _db;

        public InventoryRepository(InventoryDbContext db) => _db = db;

        public Task<InventoryItem?> GetByProductIdAsync(string productId, CancellationToken ct)
            => _db.InventoryItems.FirstOrDefaultAsync(x => x.ProductId == productId, ct);

        public Task UpdateStockAsync(InventoryItem item, CancellationToken ct)
            => Task.CompletedTask; // EF track rồi

        public Task<InventoryItem> AddAsync(InventoryItem inventoryItem, CancellationToken cancellationToken )
        {
            _db.InventoryItems.Add(inventoryItem);
            return Task.FromResult(inventoryItem);
        }

        public Task SaveChangesAsync(CancellationToken ct)
            => _db.SaveChangesAsync(ct);
    }
}
