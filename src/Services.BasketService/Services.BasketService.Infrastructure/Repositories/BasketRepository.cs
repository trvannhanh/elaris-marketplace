using Services.BasketService.Application.Interfaces;
using Services.BasketService.Application.Models;
using Services.BasketService.Infrastructure.Monitoring;
using StackExchange.Redis;
using System.Text.Json;

namespace Services.BasketService.Infrastructure.Repositories
{
    public class BasketRepository : IBasketRepository
    {
        private readonly IDatabase _db;
        private readonly TimeSpan BasketTTL = TimeSpan.FromDays(7);

        public BasketRepository(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        private string Key(string userId) => $"basket:{userId}";

        public async Task<List<BasketItem>> GetBasketAsync(string userId, CancellationToken ct = default)
        {
            var hashKey = Key(userId);
            var entries = await _db.HashGetAllAsync(hashKey);

            if (entries.Length == 0)
            {
                RedisMetrics.RedisMissCounter.Inc();
                return new List<BasketItem>();
            }

            RedisMetrics.RedisHitCounter.Inc();

            var items = entries.Select(e =>
                JsonSerializer.Deserialize<BasketItem>(e.Value!)!).ToList();

            // refresh TTL on read
            await _db.KeyExpireAsync(hashKey, BasketTTL);

            return items;
        }

        public async Task AddOrUpdateItemAsync(string userId, BasketItem item, CancellationToken ct = default)
        {
            var hashKey = Key(userId);

            // Save item
            await _db.HashSetAsync(hashKey,
                item.ProductId,
                JsonSerializer.Serialize(item));

            // Reset TTL every update
            await _db.KeyExpireAsync(hashKey, BasketTTL);
        }

        public async Task<bool> RemoveItemAsync(string userId, string productId, CancellationToken ct = default)
        {
            var key = Key(userId);
            await _db.HashDeleteAsync(key, productId);
            return true;
        }

        public async Task<bool> ClearBasketAsync(string userId, CancellationToken ct = default)
        {
            var key = Key(userId);
            return await _db.KeyDeleteAsync(key);
        }
    }
}
