
using Microsoft.EntityFrameworkCore;
using Services.InventoryService.Application.DTOs;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Domain.Entities;
using Services.InventoryService.Infrastructure.Persistence;
using System.Net.Http.Json;

namespace Services.InventoryService.Infrastructure.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly InventoryDbContext _db;
        private readonly HttpClient _http;

        public InventoryRepository(InventoryDbContext db, HttpClient http)
        {
            _db = db;
            _http = http;
        }

        public async Task<InventoryItem?> GetByProductIdAsync(string productId, CancellationToken cancellationToken = default)
        {
            return await _db.InventoryItems
                .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
        }

        public async Task<bool> HasStockAsync(string productId, int quantity, CancellationToken cancellationToken = default)
        {
            var inventory = await GetByProductIdAsync(productId, cancellationToken);
            return inventory != null && inventory.AvailableStock >= quantity;
        }

        public async Task<OrderDto?> FetchOrderDetails(Guid orderId, CancellationToken ct = default)
        {
            return await _http.GetFromJsonAsync<OrderDto>(
                $"http://orderservice:8080/api/orders/{orderId}", ct);
        }

        public async Task DecreaseStockAsync(string productId, int quantity, CancellationToken cancellationToken = default)
        {
            var inventory = await GetByProductIdAsync(productId, cancellationToken);

            if (inventory == null)
                throw new InvalidOperationException("Inventory record not found");

            if (inventory.AvailableStock < quantity)
                throw new InvalidOperationException("Not enough stock to decrease");

            inventory.AvailableStock -= quantity;
            _db.InventoryItems.Update(inventory);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task AddAsync(InventoryItem inventory, CancellationToken cancellationToken = default)
        {
            await _db.InventoryItems.AddAsync(inventory, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
