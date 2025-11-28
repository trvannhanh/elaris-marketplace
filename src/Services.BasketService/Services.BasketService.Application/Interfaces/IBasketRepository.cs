using Services.BasketService.Application.Models;


namespace Services.BasketService.Application.Interfaces
{
    public interface IBasketRepository
    {
        Task<IEnumerable<BasketItem>> GetBasketAsync(string userId, CancellationToken ct = default);
        Task<BasketItem?> GetItemAsync(string userId, string productId, CancellationToken ct = default);
        Task AddOrUpdateItemAsync(string userId, BasketItem item, CancellationToken ct = default);
        Task<bool> RemoveItemAsync(string userId, string productId, CancellationToken ct = default);
        Task ClearBasketAsync(string userId, CancellationToken ct = default);
        Task<int> GetBasketCountAsync(string userId, CancellationToken ct = default);
        Task<decimal> GetBasketTotalAsync(string userId, CancellationToken ct = default);
        Task<bool> BasketExistsAsync(string userId, CancellationToken ct = default);
    }
}

