using Services.BasketService.Application.Interfaces;
using Services.BasketService.Application.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Services.BasketService.Infrastructure.Repositories
{
    public class BasketRepository : IBasketRepository
    {
        private readonly IDatabase _db;
        private const int BasketTtlMinutes = 30;

        public BasketRepository(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        private string Key(string userId) => $"basket:{userId}";

        public async Task<Basket?> GetBasketAsync(string userId, CancellationToken ct = default)
        {
            var key = Key(userId);
            var entries = await _db.HashGetAllAsync(key);
            if (entries.Length == 0) return null;

            return new Basket
            {
                UserId = userId,
                Items = entries.Select(e =>
                    JsonSerializer.Deserialize<BasketItem>(e.Value!)!).ToList()
            };
        }

        public async Task<bool> AddOrUpdateItemAsync(string userId, BasketItem item, CancellationToken ct = default)
        {
            var key = Key(userId);
            await _db.HashSetAsync(key, item.ProductId, JsonSerializer.Serialize(item));
            await _db.KeyExpireAsync(key, TimeSpan.FromMinutes(BasketTtlMinutes));
            return true;
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
