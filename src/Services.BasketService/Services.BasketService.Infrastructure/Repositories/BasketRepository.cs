using Services.BasketService.Application.Interfaces;
using Services.BasketService.Application.Models;
using Services.BasketService.Infrastructure.Monitoring;
using StackExchange.Redis;
using System.Text.Json;

namespace Services.BasketService.Infrastructure.Repositories
{
    public class BasketRepository : IBasketRepository
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public BasketRepository(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
        }

        private string GetBasketKey(string userId) => $"basket:{userId}";

        public async Task<IEnumerable<BasketItem>> GetBasketAsync(string userId, CancellationToken ct = default)
        {
            var key = GetBasketKey(userId);
            var data = await _db.StringGetAsync(key);

            if (data.IsNullOrEmpty)
                return Enumerable.Empty<BasketItem>();

            return JsonSerializer.Deserialize<List<BasketItem>>(data!) ?? new List<BasketItem>();
        }

        public async Task<BasketItem?> GetItemAsync(string userId, string productId, CancellationToken ct = default)
        {
            var items = await GetBasketAsync(userId, ct);
            return items.FirstOrDefault(i => i.ProductId == productId);
        }

        public async Task AddOrUpdateItemAsync(string userId, BasketItem item, CancellationToken ct = default)
        {
            var key = GetBasketKey(userId);
            var items = (await GetBasketAsync(userId, ct)).ToList();

            var existingItem = items.FirstOrDefault(i => i.ProductId == item.ProductId);

            if (existingItem != null)
            {
                // Update existing item
                existingItem.Quantity = item.Quantity;
                existingItem.Price = item.Price;
                existingItem.Name = item.Name;
                existingItem.ImageUrl = item.ImageUrl;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new item
                item.AddedAt = DateTime.UtcNow;
                item.UpdatedAt = DateTime.UtcNow;
                items.Add(item);
            }

            var json = JsonSerializer.Serialize(items);
            await _db.StringSetAsync(key, json, TimeSpan.FromDays(7)); // TTL 7 days
        }

        public async Task<bool> RemoveItemAsync(string userId, string productId, CancellationToken ct = default)
        {
            var key = GetBasketKey(userId);
            var items = (await GetBasketAsync(userId, ct)).ToList();

            var item = items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
                return false;

            items.Remove(item);

            if (items.Any())
            {
                var json = JsonSerializer.Serialize(items);
                await _db.StringSetAsync(key, json, TimeSpan.FromDays(7));
            }
            else
            {
                await _db.KeyDeleteAsync(key);
            }

            return true;
        }

        public async Task ClearBasketAsync(string userId, CancellationToken ct = default)
        {
            var key = GetBasketKey(userId);
            await _db.KeyDeleteAsync(key);
        }

        public async Task<int> GetBasketCountAsync(string userId, CancellationToken ct = default)
        {
            var items = await GetBasketAsync(userId, ct);
            return items.Sum(i => i.Quantity);
        }

        public async Task<decimal> GetBasketTotalAsync(string userId, CancellationToken ct = default)
        {
            var items = await GetBasketAsync(userId, ct);
            return items.Sum(i => i.Price * i.Quantity);
        }

        public async Task<bool> BasketExistsAsync(string userId, CancellationToken ct = default)
        {
            var key = GetBasketKey(userId);
            return await _db.KeyExistsAsync(key);
        }
    }
}
